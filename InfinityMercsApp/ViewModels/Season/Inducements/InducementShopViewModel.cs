using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inducements;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Season.Inducements;
using InfinityMercsApp.ViewModels.Season.Inducements.Items;

namespace InfinityMercsApp.ViewModels.Season.Inducements;

/// <summary>
/// The deployment page's inducements pop-up: hiring troops, buying command tokens and renting gear for the contract,
/// out of the inducements the company got. It works on the deployment's <see cref="ContractInducements"/>.
/// </summary>
public sealed partial class InducementShopViewModel(IDialogService dialogs, HiredTroops hiredTroops) : ObservableObject
{
    private ContractInducements _inducements = new();
    private IReadOnlyList<ContractTrooper> _troopers = [];
    private int _companySlots;

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    /// <summary>The inducements left to spend; below zero when the deployment now gets less than was spent.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LeftText))]
    public partial int Left { get; set; }

    public string LeftText => $"{Left} of {_inducements.Received} inducements left";

    /// <summary>How many troops are hired, which the deployment's room follows.</summary>
    [ObservableProperty]
    public partial int HiredCount { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReturnCommandTokenCommand))]
    public partial int CommandTokens { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuyCommandTokenCommand))]
    public partial bool CanBuyCommandToken { get; set; }

    /// <summary>What was bought, for the deployment page; empty when nothing was.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPurchases))]
    public partial string Summary { get; set; } = string.Empty;

    public bool HasPurchases => Summary.Length > 0;

    public IReadOnlyList<HireOfferItem> HireOffers { get; } = HiredTroopChart.Offers.Select(offer => new HireOfferItem(offer)).ToList();

    public IReadOnlyList<RentalOfferItem> RentalOffers { get; } = RentalChart.Offers.Select(offer => new RentalOfferItem(offer)).ToList();

    public ObservableCollection<HiredTroopRow> Hired { get; } = [];

    public ObservableCollection<RentalRow> Rentals { get; } = [];

    /// <summary>
    /// Follows the deployment: what it gets and has bought, the company troopers deployed, and the slots they take.
    /// </summary>
    public void Update(ContractInducements inducements, IReadOnlyList<ContractTrooper> troopers, int companySlots)
    {
        _inducements = inducements;
        _troopers = troopers;
        _companySlots = companySlots;
        Refresh();
    }

    [RelayCommand]
    private void Open() => IsOpen = true;

    [RelayCommand]
    private void Close() => IsOpen = false;

    /// <summary>Hires the row's trooper in the loadout picked, when the army data has one.</summary>
    [RelayCommand]
    private async Task HireAsync(HireOfferItem item)
    {
        if (!InducementShop.CanHire(_inducements, item.Offer, _companySlots))
        {
            return;
        }

        var choices = hiredTroops.ChoicesFor(item.Offer);
        if (choices.Count == 0)
        {
            await dialogs.ShowAlertAsync("Not in the Army Data", $"No army in the downloaded data fields {item.Offer.Name} in that loadout.");
            return;
        }

        var choice = choices.Count == 1
            ? choices[0]
            : await dialogs.ChooseAsync($"Hire {item.Offer.Display}", "Cancel", choices.Select(option => option.Name).ToList()) is { } picked
                ? choices.First(option => option.Name == picked)
                : null;
        if (choice is null)
        {
            return;
        }

        _inducements.Hired.Add(new HiredTroop
        {
            EntryIndex = InducementShop.NextHiredIndex(_inducements),
            Offer = item.Offer.Name,
            Name = choice.Name,
            FactionId = choice.FactionId,
            UnitId = choice.UnitId,
            GroupId = choice.GroupId,
            OptionId = choice.OptionId,
            Cost = item.Offer.Cost,
            Skills = [.. item.Offer.Skills]
        });
        Refresh();
    }

    /// <summary>Lets a hired troop go, returning the gear rented for it.</summary>
    [RelayCommand]
    private void Dismiss(HiredTroopRow row)
    {
        InducementShop.Dismiss(_inducements, row.EntryIndex);
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanBuyCommandToken))]
    private void BuyCommandToken()
    {
        _inducements.CommandTokens++;
        Refresh();
    }

    private bool CanReturnCommandToken() => CommandTokens > 0;

    [RelayCommand(CanExecute = nameof(CanReturnCommandToken))]
    private void ReturnCommandToken()
    {
        _inducements.CommandTokens--;
        Refresh();
    }

    /// <summary>Rents the gear for the trooper picked, unless it already rents something for that slot.</summary>
    [RelayCommand]
    private async Task RentAsync(RentalOfferItem item)
    {
        var troopers = Troopers();
        if (!InducementShop.CanRent(_inducements, item.Offer) || troopers.Count == 0)
        {
            return;
        }

        var labels = Labels(troopers);
        if (await dialogs.ChooseAsync($"Rent {item.Offer.Name}", "Cancel", labels) is not { } picked)
        {
            return;
        }

        var trooper = troopers[labels.ToList().IndexOf(picked)];
        if (InducementShop.RentalIn(_inducements, trooper.EntryIndex, item.Offer.Slot) is { } rented)
        {
            await dialogs.ShowAlertAsync(
                "Slot Already Rented",
                $"{trooper.Name} already rents {rented.ItemName} as its {item.Offer.Slot.Key()}. Return it first.");
            return;
        }

        _inducements.Rentals.Add(new RentedGear
        {
            ItemName = item.Offer.Name,
            Slot = item.Offer.Slot.Key(),
            EntryIndex = trooper.EntryIndex,
            Cost = item.Offer.Cost
        });
        Refresh();
    }

    [RelayCommand]
    private void ReturnRental(RentalRow row)
    {
        _inducements.Rentals.Remove(row.Rental);
        Refresh();
    }

    // The deployed company troopers, then the hired troops.
    private List<ContractTrooper> Troopers() =>
        [.. _troopers, .. _inducements.Hired.Select(troop => new ContractTrooper(troop.EntryIndex, $"{troop.Offer} (hired)"))];

    // The troopers' names for a choice, numbered where two share one.
    private static IReadOnlyList<string> Labels(IReadOnlyList<ContractTrooper> troopers) =>
        troopers.Select((trooper, index) =>
                troopers.Count(other => other.Name == trooper.Name) > 1
                    ? $"{trooper.Name} ({troopers.Take(index + 1).Count(other => other.Name == trooper.Name)})"
                    : trooper.Name)
            .ToList();

    private void Refresh()
    {
        Left = InducementShop.Left(_inducements);
        HiredCount = _inducements.Hired.Count;
        CommandTokens = _inducements.CommandTokens;
        CanBuyCommandToken = InducementShop.CanBuyCommandToken(_inducements);
        foreach (var offer in HireOffers)
        {
            offer.IsEnabled = InducementShop.CanHire(_inducements, offer.Offer, _companySlots);
        }

        foreach (var offer in RentalOffers)
        {
            offer.IsEnabled = InducementShop.CanRent(_inducements, offer.Offer);
        }

        Hired.Clear();
        foreach (var troop in _inducements.Hired)
        {
            Hired.Add(new HiredTroopRow(troop.EntryIndex, troop.Name, troop.Cost));
        }

        var names = Troopers().ToDictionary(trooper => trooper.EntryIndex, trooper => trooper.Name);
        Rentals.Clear();
        foreach (var rental in _inducements.Rentals)
        {
            Rentals.Add(new RentalRow(rental, $"{rental.ItemName}: {names.GetValueOrDefault(rental.EntryIndex, "?")} ({rental.Slot})"));
        }

        Summary = string.Join(" · ", new List<string>
        {
            HiredCount == 0 ? string.Empty : $"Hired: {string.Join(", ", _inducements.Hired.Select(troop => troop.Offer))}",
            CommandTokens == 0 ? string.Empty : $"{CommandTokens} command token{(CommandTokens == 1 ? string.Empty : "s")}",
            Rentals.Count == 0 ? string.Empty : $"Rented: {string.Join(", ", Rentals.Select(row => row.Text))}"
        }.Where(part => part.Length > 0));
        OnPropertyChanged(nameof(LeftText));
    }
}
