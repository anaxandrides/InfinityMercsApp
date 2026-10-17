using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Fireteams;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using NSubstitute;
using MetadataFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

/// <summary>
/// Services for a company selection screen over the PanOceania army: faction 101 (vanilla) and 102
/// (a sectorial whose data is PanOceania's too).
/// </summary>
internal sealed class SelectionTestKit
{
    public const int Sectorial = 102;

    public INavigationService Navigation { get; } = Substitute.For<INavigationService>();
    public IDialogService Dialogs { get; } = Substitute.For<IDialogService>();
    public ICaptainConfigurationDialog CaptainDialog { get; } = Substitute.For<ICaptainConfigurationDialog>();
    public ISpecOpsProvider SpecOps { get; } = Substitute.For<ISpecOpsProvider>();
    public IAppPackageFiles PackageFiles { get; } = Substitute.For<IAppPackageFiles>();
    public IAppSettingsProvider Settings { get; } = Substitute.For<IAppSettingsProvider>();
    public ICompanyFileService CompanyFiles { get; } = Substitute.For<ICompanyFileService>();
    public ISeasonFileService SeasonFiles { get; } = Substitute.For<ISeasonFileService>();
    public RecordPaths Paths { get; } = new(Path.Combine(Path.GetTempPath(), "mercs-selection-tests"));
    public IArmyDataService ArmyData { get; } = Army101Data.Create();

    public SelectionTestKit()
    {
        var army = Army101Data.Definition;
        AddFaction(Army101Data.PanOceania, army.FireteamChart);
        AddFaction(Sectorial, CoreChart(army.FireteamChart));

        ArmyData.GetMetadataFactionById(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call => call.ArgAt<int>(0) switch
        {
            Army101Data.PanOceania => new MetadataFaction { Id = Army101Data.PanOceania, ParentId = Army101Data.PanOceania, Name = "PanOceania" },
            Sectorial => new MetadataFaction { Id = Sectorial, ParentId = Army101Data.PanOceania, Name = "Military Orders" },
            _ => null
        });
        ArmyData.GetMergedMercsArmyList(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns(call =>
            call.ArgAt<IReadOnlyCollection<int>>(0)
                .SelectMany(factionId => Resumes(factionId).Select(resume => new MercsArmyListEntry
                {
                    Resume = resume,
                    ProfileGroups = army.Units.FirstOrDefault(unit => unit.Id == resume.UnitId)?.ProfileGroups
                        ?? UnitProfileGroups.Empty,
                    SourceFactionIds = [factionId]
                }))
                .ToList());
        SpecOps.GetSpecopsUnitsByFaction(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        SpecOps.GetCCFactionFireteamValidity(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns([]);
        Settings.GetShowUnitsInInches().Returns(false);
    }

    /// <summary>Gives <paramref name="factionId"/> PanOceania's units, e.g. for a generated company faction.</summary>
    public void AddFaction(int factionId, FireteamChart? chart = null)
    {
        var army = Army101Data.Definition;
        ArmyData.GetFactionSnapshot(factionId, Arg.Any<CancellationToken>())
            .Returns(new Faction { FactionId = factionId, Filters = army.Filters, FireteamChart = chart ?? FireteamChart.Empty });
        ArmyData.GetResumeByFactionMercsOnly(factionId, Arg.Any<CancellationToken>()).Returns(_ => Resumes(factionId));
        ArmyData.GetResumeByFaction(factionId, Arg.Any<CancellationToken>()).Returns(_ => Resumes(factionId));
        ArmyData.GetUnit(factionId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call =>
            army.Units.FirstOrDefault(unit => unit.Id == call.ArgAt<int>(1)) is { } unit
                ? new Unit { FactionId = factionId, UnitId = unit.Id, Name = unit.Name, ProfileGroups = unit.ProfileGroups }
                : null);
    }

    // The sectorial fields PanOceania's fireteams as Core fireteams.
    private static FireteamChart CoreChart(FireteamChart? chart) => new()
    {
        Spec = chart?.Spec ?? [],
        Teams = (chart?.Teams ?? [])
            .Select(team => new FireteamTeam { Name = team.Name, TypeList = ["CORE"], Units = team.Units })
            .ToList()
    };

    public CompanySelectionServices Services => new(Navigation, Dialogs, CaptainDialog, ArmyData, SpecOps, PackageFiles, CompanyFiles, SeasonFiles, Paths, Settings);

    private static IReadOnlyList<Resume> Resumes(int factionId) =>
        Army101Data.Definition.Resume
            .Select(resume => new Resume
            {
                FactionId = factionId,
                UnitId = resume.Id,
                Name = resume.Name,
                Slug = resume.Slug,
                Type = resume.Type,
                Category = resume.Category
            })
            .ToList();

    /// <summary>Opens the screen at a wide layout and loads its factions.</summary>
    public static async Task<TViewModel> OpenAsync<TViewModel>(TViewModel viewModel)
        where TViewModel : CompanySelectionViewModel
    {
        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Wide);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }
}
