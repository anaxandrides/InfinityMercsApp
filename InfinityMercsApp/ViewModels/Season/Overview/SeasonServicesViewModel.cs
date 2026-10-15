using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Domain.Season.Services;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Company.Viewer;
using InfinityMercsApp.ViewModels.Season.RankUp;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// The services a company can pay for between contracts, for one trooper at a time: Advanced Therapy (one
/// injury, or all at double cost), Anger Management (Frenzy, or Impetuous at double cost), Forced Retraining
/// (a perk for another of the same tier) and Regular Training. A purchase is paid from the season's credits,
/// recorded in the latest round, and then <c>changed</c> is called.
/// </summary>
public partial class SeasonServicesViewModel : ObservableObject
{
    private readonly ISeasonFileService _seasonFiles;
    private readonly IDialogService _dialogs;
    private readonly Func<Task> _changed;
    private string? _seasonFilePath;
    private int _credits;

    public SeasonServicesViewModel(ISeasonFileService seasonFiles, IDialogService dialogs, Func<Task> changed)
    {
        _seasonFiles = seasonFiles;
        _dialogs = dialogs;
        _changed = changed;
        Options = SeasonServices.All.Select(service => new ServiceOption(service)).ToList();
    }

    public IReadOnlyList<ServiceOption> Options { get; }

    [ObservableProperty]
    private IReadOnlyList<CompanyViewerUnitListItem> _troopers = [];

    [ObservableProperty]
    private CompanyViewerUnitListItem? _selectedTrooper;

    [ObservableProperty]
    private IReadOnlyList<InjuryTableEntry> _injuries = [];

    [ObservableProperty]
    private InjuryTableEntry? _selectedInjury;

    /// <summary>The selected trooper's perks taken this season that nothing builds on.</summary>
    [ObservableProperty]
    private IReadOnlyList<PerkOption> _retrainablePerks = [];

    [ObservableProperty]
    private PerkOption? _selectedRetrainPerk;

    /// <summary>Perks of the same tier the trooper could take instead.</summary>
    [ObservableProperty]
    private IReadOnlyList<PerkOption> _replacements = [];

    [ObservableProperty]
    private PerkOption? _selectedReplacement;

    /// <summary>Shows the services for the company's troopers, keeping the trooper picked before.</summary>
    public void Show(string? seasonFilePath, SeasonFile? season, IReadOnlyList<CompanyViewerUnitListItem> troopers)
    {
        _seasonFilePath = seasonFilePath;
        _credits = SeasonEconomy.Ledger(season).CreditsBalance;
        var picked = SelectedTrooper?.EntryIndex;
        Troopers = troopers;
        SelectedTrooper = troopers.FirstOrDefault(trooper => trooper.EntryIndex == picked) ?? troopers.FirstOrDefault();
        RefreshOptions();
    }

    partial void OnSelectedTrooperChanged(CompanyViewerUnitListItem? value)
    {
        var progress = value?.Progress;
        Injuries = progress?.Record.Injuries.Select(InjuryRules.EntryFor).ToList() ?? [];
        SelectedInjury = Injuries.FirstOrDefault();
        RetrainablePerks = progress is null
            ? []
            : progress.Record.Perks
                .Where(perk => SeasonServices.CanRetrain(progress.Perks, perk))
                .Select(PerkChoices.PickFor)
                .OfType<PerkPick>()
                .Select(pick => new PerkOption(pick))
                .ToList();
        SelectedRetrainPerk = RetrainablePerks.FirstOrDefault();
        RefreshOptions();
    }

    partial void OnSelectedRetrainPerkChanged(PerkOption? value)
    {
        Replacements = value is null || SelectedTrooper is not { Progress: { } progress } trooper
            ? []
            : ReplacementsFor(trooper, progress, value.Pick);
        SelectedReplacement = Replacements.FirstOrDefault();
        RefreshOptions();
    }

    partial void OnSelectedReplacementChanged(PerkOption? value) => RefreshOptions();

    partial void OnSelectedInjuryChanged(InjuryTableEntry? value) => RefreshOptions();

    // Any perk of the same tier from the same kind of list that the trooper could take, the old one aside.
    private static IReadOnlyList<PerkOption> ReplacementsFor(CompanyViewerUnitListItem trooper, TrooperProgress progress, PerkPick old)
    {
        var owned = TrooperPerks.Owned(trooper, progress).Where(perk => !string.Equals(perk, old.NodeId, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var kind = PerkChoices.IsTagPerk(old.NodeId) ? PerkPointKind.Tag : PerkPointKind.Standard;
        return PerkChoices.ListsFor(kind)
            .SelectMany(list => PerkChoices.Selectable(list.ListId, old.Tier, owned))
            .Where(pick => pick.Tier == old.Tier && !string.Equals(pick.NodeId, old.NodeId, StringComparison.OrdinalIgnoreCase))
            .Select(pick => new PerkOption(pick))
            .ToList();
    }


    private void RefreshOptions()
    {
        foreach (var option in Options)
        {
            option.UnavailableReason = ReasonUnavailable(option.Service);
        }

        BuyCommand.NotifyCanExecuteChanged();
    }

    private string ReasonUnavailable(SeasonService service)
    {
        if (SelectedTrooper is not { Progress: { } progress } trooper)
        {
            return "Pick a trooper.";
        }

        if (_credits < service.Cost())
        {
            return $"Needs {service.Cost()} CR; {_credits} CR available.";
        }

        var characteristics = ProfileText.Lines(trooper.SavedCharacteristics);
        return service switch
        {
            SeasonService.AdvancedTherapy when SelectedInjury is null => "No injury to write off.",
            SeasonService.FullTherapy when progress.Record.Injuries.Count == 0 => "No injuries to write off.",
            SeasonService.CalmFrenzy when !Has(characteristics, InjuryEffects.Frenzy) => "Doesn't have Frenzy.",
            SeasonService.CalmImpetuous when !Has(characteristics, SeasonServices.Impetuous) => "Isn't Impetuous.",
            SeasonService.ForcedRetraining when progress.RankLevel < 1 => "Needs rank 1 or above.",
            SeasonService.ForcedRetraining when SelectedRetrainPerk is null => "No perk taken this season can be retrained.",
            SeasonService.ForcedRetraining when SelectedReplacement is null => "No perk of the same tier to retrain into.",
            SeasonService.RegularTraining when !Has(characteristics, SeasonServices.Irregular) => "Already regular.",
            _ => string.Empty
        };
    }

    private bool CanBuy(ServiceOption? option) => option is { IsAvailable: true };

    /// <summary>After confirming, pays for the service and applies it to the selected trooper.</summary>
    [RelayCommand(CanExecute = nameof(CanBuy))]
    private async Task BuyAsync(ServiceOption? option)
    {
        if (option is not { IsAvailable: true } || SelectedTrooper is not { } trooper)
        {
            return;
        }

        var service = option.Service;
        var detail = DetailFor(service);
        if (!await _dialogs.ConfirmAsync(service.Name(), $"{service.Name()} for {trooper.Name}{detail} — {service.Cost()} CR?", "Buy", "Cancel"))
        {
            return;
        }

        var injury = SelectedInjury?.Injury;
        var retrainFrom = SelectedRetrainPerk?.Pick.NodeId;
        var retrainTo = SelectedReplacement?.Pick;
        var keepsNoCover = KeepsNoCover(trooper);
        var recorded = await _seasonFiles.UpdateAsync(_seasonFilePath, season =>
        {
            SeasonRounds.CurrentMarketplace(season).Transactions.Add(new SeasonTransaction
            {
                OriginStore = ServiceStoreNames.MedicalServices,
                ItemName = $"{service.Name()}: {trooper.Name}{detail}",
                CostCr = service.Cost()
            });

            var record = season.TrooperFor(trooper.EntryIndex);
            switch (service)
            {
                case SeasonService.AdvancedTherapy when injury is { } chosen:
                    SeasonServices.RemoveInjury(record, chosen);
                    break;
                case SeasonService.FullTherapy:
                    SeasonServices.RemoveAllInjuries(record);
                    break;
                case SeasonService.CalmFrenzy or SeasonService.CalmImpetuous:
                    SeasonServices.CalmDown(record, service, keepsNoCover);
                    break;
                case SeasonService.ForcedRetraining when retrainFrom is not null && retrainTo is not null:
                    SeasonServices.Retrain(record, retrainFrom, retrainTo);
                    break;
                case SeasonService.RegularTraining:
                    SeasonServices.TrainRegular(record);
                    break;
            }
        });

        if (!recorded)
        {
            await _dialogs.ShowAlertAsync("Nothing Bought", SeasonMessages.NotSaved);
            return;
        }

        await _changed();
    }

    private string DetailFor(SeasonService service) => service switch
    {
        SeasonService.AdvancedTherapy when SelectedInjury is { } injury => $" ({injury.Label})",
        SeasonService.ForcedRetraining when SelectedRetrainPerk is { } from && SelectedReplacement is { } to => $" ({from.Name} → {to.Name})",
        _ => string.Empty
    };

    // No Cover from Aerial or a Nanoscreen can't be trained away.
    private static bool KeepsNoCover(CompanyViewerUnitListItem trooper) =>
        Has(ProfileText.Lines(trooper.SavedCharacteristics), "Aerial") ||
        ProfileText.Lines(trooper.SavedEquipment).Any(item => item.Contains("Nanoscreen", StringComparison.OrdinalIgnoreCase));

    private static bool Has(IEnumerable<string> lines, string name) =>
        lines.Any(line => string.Equals(line.Trim(), name, StringComparison.OrdinalIgnoreCase));
}
