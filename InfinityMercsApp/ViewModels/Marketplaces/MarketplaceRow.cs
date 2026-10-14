using CommunityToolkit.Mvvm.ComponentModel;

namespace InfinityMercsApp.ViewModels.Marketplaces;

/// <summary>Something a marketplace sells, with the quantity added to the cart.</summary>
public abstract class MarketplaceRow(string cartName, int costCr) : ObservableObject
{
    private int _quantity;

    /// <summary>The name shown on the row's cart line.</summary>
    public string CartName { get; } = cartName;

    public int CostCr { get; } = costCr;

    public virtual string CostDisplay => $"{CostCr}cr";

    /// <summary>How many are in the cart; never below zero.</summary>
    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, Math.Max(0, value));
    }
}
