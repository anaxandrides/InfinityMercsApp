namespace InfinityMercsApp.ViewModels.Marketplaces.Items;

/// <summary>A cart line: how many of a row are being bought and what they cost together.</summary>
public sealed record MarketplaceCartLine(string Name, int Quantity, int LineCost)
{
    public string QuantityDisplay => $"x{Quantity}";
    public string CostDisplay => $"{LineCost}cr";

    /// <summary>A line for each row with a quantity, in the order given.</summary>
    public static IReadOnlyList<MarketplaceCartLine> From(IEnumerable<MarketplaceRow> rows) =>
        rows.Where(row => row.Quantity > 0)
            .Select(row => new MarketplaceCartLine(row.CartName, row.Quantity, row.CostCr * row.Quantity))
            .ToList();
}
