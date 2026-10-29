using InfinityMercsApp.AotSmokeTests.Support;
using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Infrastructure.Json;

namespace InfinityMercsApp.AotSmokeTests.Smoke;

/// <summary>The account's API as the app calls it: a company fetched, and telemetry sent.</summary>
public static class ApiClientSmoke
{
    private static readonly Uri Api = new("https://api.example/api/");

    public static async Task RunAsync(SmokeChecks checks)
    {
        var documents = new UserDocumentsClient(
            new HttpClient(new CannedHandler("""{"documentId":"c1","revision":2,"content":{"CompanyName":"Blue Lions","CompanyType":"t","CompanyIdentifier":"c1","SourceFactions":[],"Entries":[]}}""")),
            new UserDocumentsEndpoint(Api),
            new UserDocumentTypes(UserDocumentJsonContext.Default));
        var reply = await documents.GetAsync<SavedCompanyFile>(UserDocumentKind.Company, "c1", "token");
        checks.Equal("json: company fetched", "Ok 2 Blue Lions", $"{reply.Status} {reply.Value?.Revision} {reply.Value?.Content?.CompanyName}");

        var handler = new CannedHandler(string.Empty);
        var sent = await new TelemetryApiClient(new HttpClient(handler), new TelemetryEndpoint(Api))
            .SendAsync(new TelemetryBatch { InstallId = "install-1", Events = [new TelemetryEvent { Kind = TelemetryKind.PageShown, Name = "Home" }] }, accessToken: null);
        checks.Equal("json: telemetry sent", TelemetrySendResult.Delivered, sent);
        checks.True("json: telemetry body", handler.Sent?.Contains("\"installId\":\"install-1\"", StringComparison.Ordinal) == true &&
                                              handler.Sent.Contains("\"PageShown\"", StringComparison.Ordinal));
    }
}
