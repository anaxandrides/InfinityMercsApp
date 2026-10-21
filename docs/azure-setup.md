# Azure setup

The app's online side (the API, and later the web site) runs in Azure. Everything in Azure is created by the Bicep
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
- **Application Insights** with a Log Analytics workspace.

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

## Sign-in

Sign-in uses the Entra External ID tenant `infinitymercs.onmicrosoft.com` (tenant id
`36af320a-cd91-43b1-8c24-f1c21c798455`), with Google as a provider and one sign-up/sign-in user flow.

| App registration | Client id | Notes |
| --- | --- | --- |
| Infinity Mercs API | `6d09c3c6-41d1-4c53-a256-d65e8e4af405` | Exposes `access_as_user`; access tokens version 2. The API accepts tokens for the client id or `api://` + client id. |
| Infinity Mercs Android | `0bbba93d-840e-48ce-b2d2-8007f227ba8d` | Public client with the API's `access_as_user` permission (admin consent granted). Its **Mobile and desktop applications** redirect URI `msal0bbba93d-840e-48ce-b2d2-8007f227ba8d://auth` is the one the app uses (MSAL.NET, system browser), so it doesn't depend on the signing key. Windows will need `http://localhost` added. |
| Web site | (to come) | Registered once the Blazor site is deployed and has its address. |

`infra/main.bicepparam` sets `authAuthority` (`https://infinitymercs.ciamlogin.com/{tenant id}/v2.0`) and
`authAudience` (the API's client id). The phone's side is `SignInSettings.Production` in `InfinityMercsApp.Services`,
and `MsalActivity` on Android catches the redirect.
