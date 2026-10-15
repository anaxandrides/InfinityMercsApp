using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// The season's Inventory tab: what the company owns, by category, with a button to sell each priced item
/// for half its cost. A sale is recorded in the latest round and then <c>changed</c> is called.
/// </summary>
public partial class SeasonInventoryViewModel(
    ISeasonFileService seasonFiles,
    IDialogService dialogs,
    Func<Task> changed) : ObservableObject
{
    public const string NoSeasonMessage = "No season inventory found.";
    public const string EmptyMessage = "No inventory items yet.";

    private string? _seasonFilePath;

    [ObservableProperty]
    private IReadOnlyList<InventorySection> _sections = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string _message = string.Empty;

    public bool HasMessage => Message.Length > 0;

    /// <summary>Shows what the company in <paramref name="seasonFilePath"/> owns.</summary>
    public void Show(string? seasonFilePath, SeasonFile? seasonFile, SeasonInventory inventory)
    {
        _seasonFilePath = seasonFilePath;
        Sections = inventory.Entries
            .GroupBy(entry => entry.Category)
            .Select(group => new InventorySection(group.Key.ToUpperInvariant(), group.Select(entry => new InventoryItem(entry)).ToList()))
            .ToList();
        Message = seasonFile is null ? NoSeasonMessage
            : Sections.Count == 0 ? EmptyMessage
            : string.Empty;
    }

    /// <summary>After confirming, sells one of the item for half its price.</summary>
    [RelayCommand]
    private async Task SellAsync(InventoryItem? item)
    {
        if (item is null || !item.CanSell)
        {
            return;
        }

        var price = item.Entry.SellPrice;
        var confirmed = await dialogs.ConfirmAsync(
            "Sell Equipment",
            $"Sell {item.Name} for {price} CR?",
            "Sell",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        var recorded = await seasonFiles.UpdateAsync(_seasonFilePath, season =>
            SeasonRounds.CurrentMarketplace(season).Transactions.Add(new SeasonTransaction
            {
                OriginStore = item.Entry.Sources.FirstOrDefault() ?? string.Empty,
                ItemName = item.Name,
                CostCr = -price,
                IsSale = true
            }));
        if (!recorded)
        {
            await dialogs.ShowAlertAsync("Nothing Sold", SeasonMessages.NotSaved);
            return;
        }

        await changed();
    }
}
