using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season.Peripherals;

namespace InfinityMercsApp.Services.Season;

/// <summary>
/// The company-file side of the Peripheral (Controller) augment, for the season's Team tab: peripherals move to the
/// trooper that equips it, are bought from the Tohaa army list, or are sold once no one controls them. Each change
/// saves the company, and a purchase or sale is charged to the round's market; when that fails the company is put back.
/// </summary>
public sealed class PeripheralTrades(
    ICompanyFileService companyFiles,
    ISeasonFileService seasonFiles,
    IDialogService dialogs,
    IArmyDataService armyData)
{
    /// <summary>What buying a peripheral for the Peripheral (Controller) augment is called in the marketplace.</summary>
    public const string PeripheralPrefix = "Peripheral: ";

    /// <summary>What selling one is called.</summary>
    public const string PeripheralSalePrefix = "Peripheral sold: ";

    private readonly TohaaPeripherals _peripherals = new(armyData);

    /// <summary>
    /// Detaches the peripherals of troopers losing the augment, then has the trooper gaining it pick one: an owned
    /// peripheral no one controls, or a new one bought at its cost and charged to the round's market.
    /// </summary>
    /// <returns>The company as saved; null when the pick is cancelled, can't be paid for or isn't saved, and nothing changed.</returns>
    public async Task<SavedCompanyFile?> ChangeAsync(string companyFilePath, string? seasonFilePath, PeripheralChange change)
    {
        if (await LoadAsync(companyFilePath) is not { } company)
        {
            return null;
        }

        var season = await seasonFiles.LoadAsync(seasonFilePath);
        foreach (var peripheral in company.Entries.Where(entry =>
                     entry.FromControllerAugment && entry.ParentEntryIndex is { } controller && change.LosingControl.Contains(controller)))
        {
            peripheral.ParentEntryIndex = null;
        }

        PeripheralOffer? bought = null;
        if (change.GainingControl is { } gaining)
        {
            var owned = company.Entries
                .Where(entry => entry.FromControllerAugment && SeasonTroopers.ControllerOf(entry, season) is null && season?.IsRetired(entry.EntryIndex) != true)
                .ToList();
            var offers = _peripherals.Offers();
            List<string> choices = [.. owned.Select(OwnedChoice), .. offers.Select(offer => offer.Display)];
            if (choices.Count == 0)
            {
                await dialogs.ShowAlertAsync("No Peripherals", "The army data has no Tohaa peripherals to choose from; update the data and try again.");
                return null;
            }

            var choice = await dialogs.ChooseAsync($"Peripheral for {change.GainingName}", "Cancel", choices);
            if (owned.FirstOrDefault(entry => OwnedChoice(entry) == choice) is { } reattached)
            {
                reattached.ParentEntryIndex = gaining;
            }
            else if (offers.FirstOrDefault(offer => offer.Display == choice) is { } offer)
            {
                var credits = SeasonEconomy.Ledger(season).CreditsBalance;
                var nextIndex = company.Entries.Count == 0 ? 0 : company.Entries.Max(entry => entry.EntryIndex) + 1;
                if (offer.Cost > credits)
                {
                    await dialogs.ShowAlertAsync("Not Enough CR", $"{offer.Name} costs {offer.Cost} CR; the company has {credits} CR.");
                    return null;
                }

                if (_peripherals.EntryFor(offer, nextIndex, gaining) is not { } added)
                {
                    return null;
                }

                company.Entries.Add(added);
                bought = offer;
            }
            else
            {
                return null;
            }
        }

        if (!await SaveAsync(companyFilePath, company))
        {
            return null;
        }

        if (bought is { } paidFor && !await seasonFiles.UpdateAsync(seasonFilePath, seasonFile =>
                SeasonRounds.CurrentMarketplace(seasonFile).Transactions.Add(new SeasonTransaction
                {
                    OriginStore = ServiceStoreNames.AdditionalRecruitment,
                    ItemName = PeripheralPrefix + paidFor.Name,
                    CostCr = paidFor.Cost
                })))
        {
            company.Entries.RemoveAt(company.Entries.Count - 1);
            await SaveAsync(companyFilePath, company);
            await dialogs.ShowAlertAsync("Peripheral Not Bought", SeasonMessages.NotSaved);
            return null;
        }

        return company;

        static string OwnedChoice(SavedCompanyEntry entry) => $"{SeasonTroopers.NameOf(entry)} (owned, #{entry.EntryIndex})";
    }

    /// <summary>Sells a peripheral no trooper controls, once confirmed, for half its cost.</summary>
    /// <returns>The company as saved; null when the sale is declined or isn't saved.</returns>
    public async Task<SavedCompanyFile?> SellAsync(string companyFilePath, string? seasonFilePath, int entryIndex)
    {
        if (await LoadAsync(companyFilePath) is not { } company ||
            company.Entries.FirstOrDefault(entry => entry.EntryIndex == entryIndex && entry.FromControllerAugment) is not { } peripheral)
        {
            return null;
        }

        var name = SeasonTroopers.NameOf(peripheral);
        var price = (int)Math.Ceiling(peripheral.Cost / 2.0);
        if (!await dialogs.ConfirmAsync("Sell Peripheral", $"Sell {name} for {price} CR? It leaves the company, with its XP and perks.", "Sell", "Cancel"))
        {
            return null;
        }

        company.Entries.Remove(peripheral);
        if (!await SaveAsync(companyFilePath, company))
        {
            return null;
        }

        if (!await seasonFiles.UpdateAsync(seasonFilePath, seasonFile =>
                SeasonRounds.CurrentMarketplace(seasonFile).Transactions.Add(new SeasonTransaction
                {
                    OriginStore = ServiceStoreNames.AdditionalRecruitment,
                    ItemName = PeripheralSalePrefix + name,
                    CostCr = -price,
                    IsSale = true
                })))
        {
            company.Entries.Add(peripheral);
            await SaveAsync(companyFilePath, company);
            await dialogs.ShowAlertAsync("Peripheral Not Sold", SeasonMessages.NotSaved);
            return null;
        }

        return company;
    }

    private async Task<SavedCompanyFile?> LoadAsync(string companyFilePath)
    {
        try
        {
            return await companyFiles.LoadAsync(companyFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine($"Reading the company failed: {ex.Message}");
            await dialogs.ShowAlertAsync("Company Not Read", "The company file couldn't be read.");
            return null;
        }
    }

    private async Task<bool> SaveAsync(string companyFilePath, SavedCompanyFile company)
    {
        try
        {
            await companyFiles.SaveAsync(companyFilePath, company);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Saving the company failed: {ex.Message}");
            await dialogs.ShowAlertAsync("Company Not Saved", "The company file couldn't be saved, so nothing changed.");
            return false;
        }
    }
}
