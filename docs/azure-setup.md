# Azure setup

The app's online side (the API and the web site) runs in Azure. Everything in Azure is created by the Bicep
in `infra/`, deployed from GitHub Actions. A few things have to exist before the first deploy, and they're set up
once by hand.

## What's in Azure

`infra/main.bicep` creates the resource group `rg-infmercs-prod` in East US, holding:

- **Function app** `func-infmercs-prod` (Flex Consumption, .NET 10): the API in `InfinityMercsApp.Api`, at
  `https://func-infmercs-prod.azurewebsites.net/api/`. One instance is kept warm (`alwaysReadyInstances`). The phone
  app has this address in `ArmyMirrorEndpoint.Production`, so renaming the app means changing it there too.
- **Managed identity** `id-infmercs-api-prod`: the Function app runs as it. It is the only thing with access to the
  storage account, Cosmos DB and Application Insights, so there are no keys or connection strings anywhere.
- **Storage account**: the Functions host's state, the deployment package (`app-package`) and the army data
  payloads (`army-data`). Shared keys are off.
- **Cosmos DB** (serverless): the `mercs` database, holding the army data snapshots (`army`) and later companies,
  seasons and leagues. Key access is off.
- **Application Insights** with a Log Analytics workspace, capped at 1 GB of ingestion a day (`dailyCapGb` in
  `infra/modules/monitoring.bicep`): past it, nothing more is kept until the next day.
- **Static Web App** `stapp-infmercs-prod` (Free, East US 2, since Static Web Apps run in only a few regions): the
  Blazor WebAssembly site in `InfinityMercsApp.Web`. Its address is `https://{generated name}.azurestaticapps.net`;
  the Infrastructure workflow prints it as `webHostName`, and the API allows calls from it (CORS) along with the web
  project's local dev server.

None of the IDs below are secrets: tenant, subscription and client IDs are identifiers, and the phone and web apps ship
some of them to every user anyway. Access is protected by the identities and their roles, not by hiding the IDs.

## One-time setup: the deploy identity

GitHub Actions signs in to Azure as an app registration that trusts this repository's `production` environment
(OIDC, so no secret is stored in GitHub). In Azure Cloud Shell:

```bash
SUB=$(az account show --query id -o tsv)
TENANT=$(az account show --query tenantId -o tsv)

APP=$(az ad app create --display-name infinitymercs-deploy --query appId -o tsv)
az ad sp create --id "$APP"

# GitHub names this repository by owner and repository id as well as by name (repo:owner@id/name@id), so a rename
# can't hand the trust to another repository. The subject must match exactly; a failed sign-in (AADSTS700213) prints
# the one GitHub presented.
az ad app federated-credential create --id "$APP" --parameters '{
  "name": "github-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:anaxandrides@48453148/InfinityMercsApp@1416092523:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}'

# Contributor creates the resources; RBAC Administrator lets the Bicep give the API's identity its roles.
az role assignment create --assignee "$APP" --role Contributor --scope "/subscriptions/$SUB"
az role assignment create --assignee "$APP" --role "Role Based Access Control Administrator" --scope "/subscriptions/$SUB"

echo "AZURE_CLIENT_ID=$APP"
echo "AZURE_TENANT_ID=$TENANT"
echo "AZURE_SUBSCRIPTION_ID=$SUB"
```

Then in the GitHub repository, under **Settings**:

1. **Environments → New environment**: `production`. Optionally add yourself as a required reviewer, so every deploy
   waits for approval.
2. **Secrets and variables → Actions → Variables** (not Secrets): add `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and
   `AZURE_SUBSCRIPTION_ID` with the values echoed above.

## Deploying

- **Infrastructure** (`.github/workflows/infra.yml`): runs when `infra/` changes on `main`, or by hand from the
  Actions tab. Run it first.
- **API** (`.github/workflows/api.yml`): tests on every pull request that touches the API or the projects it uses; on
  `main` it also deploys and refreshes the army data mirror.
- **Web** (`.github/workflows/web.yml`): tests and publishes the site on every pull request that touches it; on `main`
  it also deploys it, reading the Static Web App's deployment token with the deploy identity at deploy time.

GitHub only offers "Run workflow" for workflows that are on `main`, so the first run happens after this branch is
merged.

## Running the API locally

1. Install [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) and
   [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite), and start Azurite.
2. Start the [Cosmos DB emulator](https://learn.microsoft.com/azure/cosmos-db/emulator) and create the `mercs`
   database with an `army` container partitioned on `/partition`.
3. Copy `InfinityMercsApp.Api/local.settings.sample.json` to `local.settings.json` (it is git-ignored) and fill in the
   emulator's connection string.
4. `cd InfinityMercsApp.Api && func start`, then trigger the refresh once:
   `curl -X POST http://localhost:7071/admin/functions/RefreshArmyData -H "Content-Type: application/json" -d '{"input":""}'`
5. `curl http://localhost:7071/api/army/manifest`

## The army data API

The mirror copies Corvus Belli's army data every six hours, keeping each payload exactly as served, so the app's
existing readers work on it unchanged. The phone reads from it first and checks its manifest, so it only downloads
armies whose version changed; when the mirror can't be reached it goes to Corvus Belli directly, and leaves the mirror
alone for ten minutes.

**Versions.** Each payload is kept in blob storage under its SHA-256 (`army-data/payloads/{hash}.json.gz`) and never
overwritten or deleted. Each distinct set of payloads is a *snapshot*: a Cosmos DB document in the `army` container
listing the metadata's hash and each army's version and hash. Its id is worked out from those hashes, so the same
army data always has the same id. The current snapshot is also kept as the document `latest`. A company can pin a
snapshot id and read exactly that army data for good.

| Route | What |
| --- | --- |
| `GET /api/army/manifest` | The current snapshot: its id, when the mirror last checked, and the hash of the metadata and each army (and each army's version). Never cached. |
| `GET /api/army/metadata` | The current metadata payload. |
| `GET /api/army/units/{factionId}` | The current army payload of one faction. |
| `GET /api/army/snapshots/{snapshotId}` | A pinned snapshot's manifest. |
| `GET /api/army/snapshots/{snapshotId}/metadata` | A pinned snapshot's metadata payload. |
| `GET /api/army/snapshots/{snapshotId}/units/{factionId}` | A pinned snapshot's army payload. |

Payloads are sent gzipped to clients that take gzip. Their ETag is their hash, so a client that sends `If-None-Match`
gets `304 Not Modified` when its copy is current. A snapshot's routes are cached for a year, since they never change.

## The companies and seasons API

Each signed-in user keeps their own companies and seasons, in the Cosmos DB `user-data` container, partitioned by
their user id. Files are the app's own types (`SavedCompanyFile` and `SeasonFile` in `InfinityMercsApp.Domain`), so
the API, the app and the web site always agree on their shape.

| Route | What |
| --- | --- |
| `GET /api/me/companies`, `GET /api/me/seasons` | The user's files, without their content, deleted ones included. |
| `GET /api/me/companies/{id}`, `GET /api/me/seasons/{id}` | One file. |
| `PUT /api/me/companies/{id}`, `PUT /api/me/seasons/{id}` | Saves a file: `{ "baseRevision": n, "armySnapshotId": "...", "content": { ... } }`. |
| `DELETE /api/me/companies/{id}?baseRevision=n` (and seasons) | Deletes a file. |

- **Checks:** a file missing a required field (`[JsonRequired]` on the model, e.g. a company's name, factions or
  troopers) or with a null where none is allowed is refused with `400`, so a save from an app that left something
  out can't wipe it. A company also needs exactly one Captain and unique entry indexes; a season needs its company's
  name and identifier. The `400` lists the problems.
- **Ids** are chosen by the app that creates the file (a GUID), so a phone can create files offline.
- **Revisions:** each save adds one to the file's revision, and says which revision it builds on (`0` for a new
  file). If another device saved since, the save is refused with `409 Conflict` and the current file, for the app to
  merge with. Saves are checked against Cosmos DB's ETag as well, so two saves at once can't both win.
- **Deleting** keeps the file as an empty, deleted revision, so the user's other devices learn of it. Saving on its
  revision brings it back.
- **Pinning:** `armySnapshotId` records the army data snapshot the file was built with.
- **Size:** bodies of up to 1.5 MB are accepted.
- **Sign-in:** every route needs `Authorization: Bearer <token>` from the sign-in tenant, issued for the API. Until
  `authAuthority` and `authAudience` are set in `infra/main.bicepparam`, every request gets `401 Unauthorized`.

## App telemetry

The app reports to `POST /api/telemetry` rather than to Application Insights itself, so no connection string ships in
the app. The API checks each batch and writes it to Application Insights as its managed identity.

- **What:** pages shown (with the Android Java object count, which hangs the app past roughly 40,000), crashes (type,
  message and stack trace), startup times (`FirstPage` and `MainScreen`, from launch) and failed syncs, with the app
  version, platform, OS version and device model. Never companies, names or account details. A random install id,
  made on first launch, ties an install's reports together.
- **Who can post:** anyone; there is no key to keep. A batch is refused with `400` unless it is the known shape
  (`TelemetryBatch` in `InfinityMercsApp.ApiClient`): a GUID install id, 1 to 200 events of known kinds, text within
  `TelemetryLimits`, times within the last 30 days. Bodies of up to 1 MB are read. A sign-in token, when sent, adds
  the user id.
- **Where:** records carry the cloud role name `app`. Crashes are in `exceptions`; the rest are in `customEvents`,
  named `PageShown`, `Startup` and `SyncFailure`, with the page or milestone in `customDimensions.Name` and the
  number in `customMeasurements` (`JavaObjects` or `Milliseconds`). The install id is `user_Id`, the signed-in user
  `user_AuthenticatedId`. For example:

  ```kusto
  customEvents
  | where cloud_RoleName == "app" and name == "PageShown"
  | summarize max(todouble(customMeasurements.JavaObjects)) by tostring(customDimensions.Name), bin(timestamp, 1h)
  ```
- **On the device:** events go to a file per session under `Telemetry/Outbox` in the app's data (2 MB at most, the
  oldest dropped), sent every five minutes, when the app goes to the background, and on the next launch after a
  crash. Settings has a switch to turn it off, which also deletes what is waiting.
- **Google Play:** declare crash logs and diagnostics (collected, not shared, not linked to identity unless signed
  in) in the Data safety form.

## Sign-in

Sign-in uses the Entra External ID tenant `infinitymercs.onmicrosoft.com` (tenant id
`36af320a-cd91-43b1-8c24-f1c21c798455`), with Google as a provider and one sign-up/sign-in user flow.

A user flow only applies to the app registrations added to it (**User flows → the flow → Applications → Add
application**), so the app below must be listed there. An app that isn't gets the tenant's bare default sign-in
instead: existing accounts only, no Google button and no sign-up link, with the message "This account does not exist
in this organization".

Google sends a sign-in back to Entra, not to the app, so the Google OAuth client (Google Cloud console → APIs &
services → Credentials) needs Entra's addresses as its **Authorized redirect URIs**, or Google answers
`redirect_uri_mismatch`. From [Add Google as an identity provider](https://learn.microsoft.com/entra/external-id/customers/how-to-google-federation-customers),
filled in for this tenant:

```
https://login.microsoftonline.com
https://login.microsoftonline.com/te/36af320a-cd91-43b1-8c24-f1c21c798455/oauth2/authresp
https://login.microsoftonline.com/te/infinitymercs.onmicrosoft.com/oauth2/authresp
https://36af320a-cd91-43b1-8c24-f1c21c798455.ciamlogin.com/36af320a-cd91-43b1-8c24-f1c21c798455/federation/oidc/accounts.google.com
https://36af320a-cd91-43b1-8c24-f1c21c798455.ciamlogin.com/infinitymercs.onmicrosoft.com/federation/oidc/accounts.google.com
https://infinitymercs.ciamlogin.com/36af320a-cd91-43b1-8c24-f1c21c798455/federation/oauth2
https://infinitymercs.ciamlogin.com/infinitymercs.onmicrosoft.com/federation/oauth2
```

The OAuth consent screen's **Authorized domains** include `ciamlogin.com` and `microsoftonline.com`. The Google
client's id and secret are entered only in Entra (External Identities → All identity providers → Google), never in
this repository.

| App registration | Client id | Notes |
| --- | --- | --- |
| Infinity Mercs API | `6d09c3c6-41d1-4c53-a256-d65e8e4af405` | Exposes `access_as_user`; access tokens version 2. The API accepts tokens for the client id or `api://` + client id. |
| Infinity Mercs Android | `0bbba93d-840e-48ce-b2d2-8007f227ba8d` | Public client with the API's `access_as_user` permission (admin consent granted), used by the phone app, the Windows app and the web site. **Mobile and desktop applications** redirect URIs: `msal0bbba93d-840e-48ce-b2d2-8007f227ba8d://auth` (Android, MSAL.NET through the system browser, so it doesn't depend on the signing key) and `http://localhost` (Windows). **Single-page application** redirect URIs (the web site): `https://{web host}/authentication/login-callback` and `https://{web host}/authentication/logout-callback`, plus the same two on `https://localhost:7154` for local runs. |

`infra/main.bicepparam` sets `authAuthority` (`https://infinitymercs.ciamlogin.com/{tenant id}/v2.0`) and
`authAudience` (the API's client id). The phone's side is `SignInSettings.Production` in `InfinityMercsApp.Services`,
and `MsalActivity` on Android catches the redirect. The web site's is `InfinityMercsApp.Web/wwwroot/appsettings.json`.
