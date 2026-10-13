using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.UnitTests.ViewModels;

/// <summary>Records navigation instead of performing it.</summary>
internal sealed class FakeNavigationService : INavigationService
{
    public List<(string Route, IDictionary<string, object>? Parameters)> Navigations { get; } = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null) => Record(route, routeParameters);

    public Task PushAsync(string route, IDictionary<string, object>? routeParameters = null) => Record(route, routeParameters);

    public Task PopAsync() => Record("..", null);

    public Task PushModalAsync(Page page) => Record($"modal:{page.GetType().Name}", null);

    public Task PopModalAsync() => Record("modal:pop", null);

    private Task Record(string route, IDictionary<string, object>? parameters)
    {
        Navigations.Add((route, parameters));
        return Task.CompletedTask;
    }
}

/// <summary>Season files held in memory, keyed by path.</summary>
internal sealed class FakeSeasonFileService : ISeasonFileService
{
    public Dictionary<string, SeasonFile> Files { get; } = [];

    public Task<SeasonFile?> LoadAsync(string? filePath) =>
        Task.FromResult(filePath is not null && Files.TryGetValue(filePath, out var file) ? file : null);

    public Task<bool> SaveAsync(string? filePath, SeasonFile? seasonFile)
    {
        if (filePath is null || seasonFile is null)
        {
            return Task.FromResult(false);
        }

        Files[filePath] = seasonFile;
        return Task.FromResult(true);
    }

    public Task<bool> UpdateLatestRoundAsync(string? filePath, Action<SeasonRound> updater)
    {
        if (filePath is null || !Files.TryGetValue(filePath, out var file) || file.Rounds.Count == 0)
        {
            return Task.FromResult(false);
        }

        updater(file.Rounds.OrderByDescending(r => r.RoundIndex).First());
        return Task.FromResult(true);
    }
}
