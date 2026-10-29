using InfinityMercsApp.AotSmokeTests.Support;
using InfinityMercsApp.Services.Glossary;
using InfinityMercsApp.Services.Stores;

namespace InfinityMercsApp.AotSmokeTests.Smoke;

/// <summary>The app's bundled content: the market files, and the glossary rendered by Markdig.</summary>
public static class ContentSmoke
{
    public static async Task RunAsync(SmokeChecks checks, RepoFolder repo)
    {
        var packageFiles = new RepoPackageFiles(repo);
        var store = await new StoreProvider(packageFiles).GetStoreByNameAsync("Alpha Sec");
        checks.True("json: market items", store?.Items.Count > 0);
        checks.Note("json: Alpha Sec items", store?.Items.Count);

        var index = MercsGlossary.ParseIndex(await packageFiles.ReadTextAsync(MercsGlossary.IndexFile));
        checks.True("json: glossary entries", index.Count > 0);
        checks.Note("json: glossary entries", index.Count);

        var entry = index.First(item => !item.IsHeader && item.MarkdownFile.Length > 0);
        var page = MercsGlossary.MarkdownPage(await packageFiles.ReadTextAsync(MercsGlossary.MarkdownPath(entry)));
        checks.True("markdig: glossary page rendered", page.Contains("<p>", StringComparison.Ordinal));
        checks.Note("markdig: page length", page.Length);
    }
}
