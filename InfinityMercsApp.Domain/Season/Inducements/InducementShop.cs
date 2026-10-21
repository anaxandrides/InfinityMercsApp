using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Season.Inducements;

/// <summary>
/// What a company's inducements can still buy for a contract: hired troops while there is room for them, up to two
/// command tokens, and rented gear within each rental's AVA. Everything is paid for out of what it got.
/// </summary>
public static class InducementShop
{
    public static int Spent(ContractInducements inducements) =>
        inducements.Hired.Sum(troop => troop.Cost) +
        inducements.CommandTokens * InducementRules.CommandTokenCost +
        inducements.Rentals.Sum(rental => rental.Cost);

    /// <summary>What is left to spend; below zero when the deployment changed and now gets less than was spent.</summary>
    public static int Left(ContractInducements inducements) => inducements.Received - Spent(inducements);

    /// <summary>Hired troops take the deployment up to seven troopers at most.</summary>
    public static bool HasRoomToHire(ContractInducements inducements, int companySlots) =>
        companySlots + inducements.Hired.Count < InducementRules.TrooperLimitWithHired;

    public static bool CanHire(ContractInducements inducements, HiredTroopOffer offer, int companySlots) =>
        Left(inducements) >= offer.Cost && HasRoomToHire(inducements, companySlots);

    public static bool CanBuyCommandToken(ContractInducements inducements) =>
        inducements.CommandTokens < InducementRules.MaxCommandTokens && Left(inducements) >= InducementRules.CommandTokenCost;

    /// <summary>How many of the gear are rented for the contract.</summary>
    public static int RentedCount(ContractInducements inducements, string itemName) =>
        inducements.Rentals.Count(rental => string.Equals(rental.ItemName, itemName, StringComparison.OrdinalIgnoreCase));

    public static bool CanRent(ContractInducements inducements, RentalOffer offer) =>
        Left(inducements) >= offer.Cost && (offer.Ava is not { } ava || RentedCount(inducements, offer.Name) < ava);

    /// <summary>The gear rented into the trooper's slot, if any: a trooper takes one rental a slot.</summary>
    public static RentedGear? RentalIn(ContractInducements inducements, int entryIndex, GearSlot slot) =>
        inducements.Rentals.FirstOrDefault(rental => rental.EntryIndex == entryIndex && rental.Slot == slot.Key());

    /// <summary>The index for the next hired troop: below every one hired so far.</summary>
    public static int NextHiredIndex(ContractInducements inducements) =>
        inducements.Hired.Count == 0 ? -1 : inducements.Hired.Min(troop => troop.EntryIndex) - 1;

    /// <summary>Lets a hired troop go, with the gear rented for it.</summary>
    public static void Dismiss(ContractInducements inducements, int entryIndex)
    {
        inducements.Hired.RemoveAll(troop => troop.EntryIndex == entryIndex);
        inducements.Rentals.RemoveAll(rental => rental.EntryIndex == entryIndex);
    }

    /// <summary>Returns the gear rented for company troopers that no longer deploy; hired troops keep theirs.</summary>
    public static void KeepDeployed(ContractInducements inducements, IReadOnlySet<int> deployedEntryIndices) =>
        inducements.Rentals.RemoveAll(rental => rental.EntryIndex >= 0 && !deployedEntryIndices.Contains(rental.EntryIndex));

    /// <summary>The rented gear as a trooper's season gear: it takes the place of what the slot held for the contract.</summary>
    public static List<SeasonUnitGear> WithRentals(IEnumerable<SeasonUnitGear> gear, IEnumerable<RentedGear> rentals)
    {
        var rented = rentals.ToList();
        return gear.Where(item => rented.All(rental => !string.Equals(rental.Slot, item.Slot, StringComparison.OrdinalIgnoreCase)))
            .Concat(rented.Select(rental => new SeasonUnitGear { Slot = rental.Slot, ItemName = rental.ItemName }))
            .ToList();
    }
}
