using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class DeploymentRosterTests : IDisposable
{
    private const string SeasonPath = "/seasons/s.json";

    private readonly TempRecords _records = new();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private readonly DeploymentRoster _roster;

    public DeploymentRosterTests() => _roster = new DeploymentRoster(Army101Data.Create(), _seasonFiles);

    public void Dispose() => _records.Dispose();

    private const string Company = """
        {
          "improvedCaptainStats": { "isEnabled": true, "weaponChoice1": "(+2) - Pistol", "skillChoice1": "-", "equipmentChoice1": "Albedo" },
          "entries": [
            { "entryIndex": 2, "name": "Fusilier (Hacker)", "baseUnitName": "Fusilier (Hacker)", "sourceFactionId": 101, "cost": 10, "experiencePoints": 5,
              "CurrentWeaponCodes": [ { "id": 33 }, { "id": 6 } ], "CurrentCharacteristicCodes": [ { "id": 4 } ],
              "Current BS": "12", "Current VITA/STR": "1" },
            { "entryIndex": 1, "name": "Trooper", "customName": " Kestrel ", "isLieutenant": true, "sourceFactionId": 101,
              "CurrentSkillCodes": [ { "id": 86 } ], "CurrentWeaponCodes": [ { "id": 33, "extra": [ 298 ] } ], "Current VITA/STR": "2" },
            { "entryIndex": 3, "isPeripheralUnit": true, "sourceFactionId": 101 },
            { "entryIndex": 4, "name": "Tag", "unitTypeCode": "TAG", "sourceFactionId": 2003 }
          ]
        }
        """;

    private Task<IReadOnlyList<DeploymentUnitItem>> LoadAsync(IReadOnlySet<int>? deployed = null) =>
        _roster.LoadAsync(_records.WriteCompany("c.json", Company), SeasonPath, showUnitsInInches: true, deployed);

    [Fact]
    public async Task ListsTheLieutenantFirstAndLeavesOutPeripherals()
    {
        var units = await LoadAsync();

        Assert.Equal([1, 2, 4], units.Select(u => u.EntryIndex));
        Assert.Equal("Kestrel", units[0].Name);
        Assert.Equal("Lieutenant", units[0].Subtitle);
        Assert.True(units[0].IsChecked);
        Assert.False(units[1].IsChecked);
    }

    [Fact]
    public async Task OnlyTheDeployedEntriesWhenGiven()
    {
        var units = await LoadAsync(new HashSet<int> { 2 });

        Assert.Equal([2], units.Select(u => u.EntryIndex));
    }

    [Fact]
    public async Task ResolvesProfileCodesFromTheArmyData()
    {
        var fusilier = (await LoadAsync())[1];

        Assert.Equal("Fusilier", fusilier.BaseUnitDisplayName);
        Assert.Equal("Combi Rifle", fusilier.RangedWeapons);
        Assert.Equal("AP CC Weapon", fusilier.MeleeWeapons);
        Assert.True(fusilier.IsIrregular);
        Assert.False(fusilier.IsImpetuous);
        Assert.Equal(15, fusilier.Renown);
        Assert.Equal("12", fusilier.UnitBs);
        Assert.Equal("SVGCache/units/101-0.svg", fusilier.PackagedLogoPath);
    }

    [Fact]
    public async Task TheLieutenantGetsCaptainUpgradesAndCodeExtras()
    {
        var lieutenant = (await LoadAsync())[0];

        Assert.Equal($"Combi Rifle (+1){Environment.NewLine}Pistol", lieutenant.RangedWeapons);
        Assert.Contains("Albedo", lieutenant.Equipment);
        Assert.True(lieutenant.HasNwi);
        Assert.Equal(2, lieutenant.StartingVitality);
        Assert.Equal("SVGCache/NonCBIcons/noun-captain-8115950.svg", lieutenant.CaptainIconPackagedPath);
    }

    [Fact]
    public async Task ATagCompanyTagTakesTwoSlots()
    {
        var tag = (await LoadAsync())[2];

        Assert.True(tag.IsTagFromTagCompany);
        Assert.Equal(2, tag.SlotCost);
        Assert.Equal(Stat.Str, tag.VitalityStat);
    }

    [Fact]
    public async Task TheStatlineListsEveryStatInOrder()
    {
        var tag = (await LoadAsync())[2];

        Assert.Equal<Stat>(
            [Stat.Mov, Stat.Cc, Stat.Bs, Stat.Ph, Stat.Wip, Stat.Arm, Stat.Bts, Stat.Str, Stat.S],
            tag.Statline.Select(v => v.Stat));
        Assert.Equal("STR", tag.VitalityHeader);
    }

    [Fact]
    public async Task AppliesSeasonGear()
    {
        _seasonFiles.LoadAsync(SeasonPath).Returns(new SeasonFile
        {
            UnitGear = new()
            {
                [2] =
                [
                    new SeasonUnitGear { Slot = "Augments", ItemName = "BS = 14" },
                    new SeasonUnitGear { Slot = "Augments", ItemName = "Neurocinetics" },
                    new SeasonUnitGear { Slot = "Roles", ItemName = "Forward Observer" },
                    new SeasonUnitGear { Slot = "Sidearm", ItemName = "Knife" },
                    new SeasonUnitGear { Slot = "Primary", ItemName = "Spitfire" }
                ]
            }
        });

        var fusilier = (await LoadAsync())[1];

        Assert.Equal("14", fusilier.UnitBs);
        Assert.Equal($"Neurocinetics{Environment.NewLine}Forward Observer", fusilier.Skills);
        Assert.Equal($"Combi Rifle{Environment.NewLine}Spitfire", fusilier.RangedWeapons);
        Assert.Equal($"AP CC Weapon{Environment.NewLine}Knife", fusilier.MeleeWeapons);
    }

    [Fact]
    public async Task AMissingCompanyHasNoUnits()
    {
        Assert.Empty(await _roster.LoadAsync("/nowhere/c.json", null, true));
    }

    [Theory]
    [InlineData("(+2) - Pistol", "Pistol")]
    [InlineData("-", null)]
    [InlineData("  ", null)]
    public void CaptainChoicesDropTheirCost(string choice, string? expected) =>
        Assert.Equal(expected is null ? [] : [expected], DeploymentRoster.CaptainUpgrades.Choices(choice));

    [Fact]
    public void RepurposedMiningTagsUseTheTagCompany()
    {
        var entry = JsonSerializer.Deserialize<InfinityMercsApp.Views.Common.SavedCompanyEntry>(
            """{"sourceFactionId":101,"baseUnitName":"Turtlemek"}""",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        Assert.Equal(DeploymentRoster.TagCompanyFactionId, DeploymentRoster.EffectiveSourceFactionId(entry));
    }
}

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
