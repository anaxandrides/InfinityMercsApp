using CommunityToolkit.Mvvm.ComponentModel;

namespace InfinityMercsApp.ViewModels.Marketplaces.Items;

/// <summary>Something a marketplace sells, with the quantity added to the cart.</summary>
public abstract class MarketplaceRow(string cartName, int costCr) : ObservableObject
{
    private int _quantity;
    private bool _isAffordable = true;

    /// <summary>The name shown on the row's cart line.</summary>
    public string CartName { get; } = cartName;

    public int CostCr { get; } = costCr;

    /// <summary>The SWC it also costs, if any.</summary>
    public virtual decimal? CostSwc => null;

    /// <summary>The inventory category it is kept under once bought.</summary>
    public abstract string InventoryCategory { get; }

    public virtual string CostDisplay => $"{CostCr}cr";

    /// <summary>How many are in the cart; never below zero.</summary>
    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, Math.Max(0, value));
    }

    /// <summary>Whether another one fits in what is left to spend.</summary>
    public bool IsAffordable
    {
        get => _isAffordable;
        set => SetProperty(ref _isAffordable, value);
    }
}
