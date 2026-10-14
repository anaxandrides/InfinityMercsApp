using System.Text.Json;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Season;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season;

public sealed class PlayModeViewModelTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly IAppSettingsProvider _settings = Substitute.For<IAppSettingsProvider>();
    private string _companyPath = string.Empty;

    public PlayModeViewModelTests() => _settings.GetShowUnitsInInches().Returns(true);

    public void Dispose() => _records.Dispose();

    /// <summary>A lieutenant (entry 0), then <paramref name="troopers"/> one-slot units and an optional TAG Company TAG.</summary>
    private async Task<PlayModeViewModel> OpenAsync(int troopers, bool withTag = false)
    {
        var entries = new List<object> { new { entryIndex = 0, name = "Lt", isLieutenant = true, sourceFactionId = 101 } };
        entries.AddRange(Enumerable.Range(1, troopers).Select(i => (object)new { entryIndex = i, name = $"T{i}", sourceFactionId = 101 }));
        if (withTag)
        {
            entries.Add(new { entryIndex = 99, name = "Tag", unitTypeCode = "TAG", sourceFactionId = 2003 });
        }

        _companyPath = _records.WriteCompany("c.json", JsonSerializer.Serialize(new { entries }));
        var viewModel = new PlayModeViewModel(
            _navigation, _dialogs, new DeploymentRoster(Army101Data.Create(), Substitute.For<ISeasonFileService>()), _settings);
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany(_companyPath, "/s.json"));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task StartsWithTheLieutenantDeployedAndSelected()
    {
        var viewModel = await OpenAsync(troopers: 2);

        Assert.Equal(1, viewModel.DeployedSlotCount);
        Assert.True(viewModel.IsEliteDeployment);
        Assert.Equal("1 / 6 slots · ELITE", viewModel.DeploymentStatusText);
        Assert.Same(viewModel.DeploymentUnits[0], viewModel.SelectedUnit);
        Assert.True(viewModel.DeploymentUnits[0].IsSelected);
    }

    [Fact]
    public async Task TheLieutenantCannotBeUnchecked()
    {
        var viewModel = await OpenAsync(troopers: 1);

        viewModel.DeploymentUnits[0].IsChecked = false;

        Assert.True(viewModel.DeploymentUnits[0].IsChecked);
    }

    [Fact]
    public async Task NoMoreThanSixSlotsCanBeFilled()
    {
        var viewModel = await OpenAsync(troopers: 4, withTag: true);

        foreach (var unit in viewModel.DeploymentUnits)
        {
            unit.IsChecked = true;
        }

        Assert.Equal(5, viewModel.DeployedSlotCount);
        Assert.False(viewModel.DeploymentUnits.Single(u => u.IsTagFromTagCompany).IsChecked);
        Assert.Equal("5 / 6 slots", viewModel.DeploymentStatusText);
        Assert.False(viewModel.IsEliteDeployment);
    }

    [Fact]
    public async Task SelectingAUnitMovesTheHighlight()
    {
        var viewModel = await OpenAsync(troopers: 1);

        viewModel.SelectUnitCommand.Execute(viewModel.DeploymentUnits[1]);

        Assert.False(viewModel.DeploymentUnits[0].IsSelected);
        Assert.True(viewModel.DeploymentUnits[1].IsSelected);
        Assert.True(viewModel.HasSelectedUnit);
    }

    [Fact]
    public async Task DeployingOpensTheGameWithTheCheckedUnits()
    {
        var viewModel = await OpenAsync(troopers: 2);
        viewModel.DeploymentUnits[2].IsChecked = true;

        await viewModel.DeployCommand.ExecuteAsync(null);

        await _navigation.Received(1).PushAsync("GameModePage", Arg.Is<IDictionary<string, object>>(p =>
            (string)p[NavigationParameters.CompanyFilePath] == _companyPath &&
            (string)p[NavigationParameters.SeasonFilePath] == "/s.json" &&
            ((IEnumerable<int>)p[NavigationParameters.DeployedIndices]).SequenceEqual(new List<int> { 0, 2 }) &&
            (bool)p[NavigationParameters.EliteDeployment]));
    }

    [Fact]
    public async Task ReloadsWhenTheDistanceUnitsChange()
    {
        var viewModel = await OpenAsync(troopers: 1);
        viewModel.DeploymentUnits[1].IsChecked = true;

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        Assert.True(viewModel.DeploymentUnits[1].IsChecked);

        _settings.GetShowUnitsInInches().Returns(false);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        Assert.False(viewModel.DeploymentUnits[1].IsChecked);
    }
}
