namespace InfinityMercsApp.Domain.Models.Stores;

public class Store
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<string> AssociatedFactions { get; set; } = [];
    public string? AssociatedType { get; set; }
    public string Alignment { get; set; } = string.Empty;
    public IReadOnlyList<StoreItem> Items { get; set; } = [];
    public IReadOnlyList<StoreTroopType> TroopTypes { get; set; } = [];
    public IReadOnlyList<StoreAugment> Augments { get; set; } = [];

    public bool IsLawful => HasAlignment(StoreAlignments.Lawful);
    public bool IsChaotic => HasAlignment(StoreAlignments.Chaotic);
    public bool IsNeutral => HasAlignment(StoreAlignments.Neutral);
    public bool IsAlwaysAvailable => HasAlignment(StoreAlignments.AlwaysAvailable);

    /// <summary>Medical Services or Additional Recruitment.</summary>
    public bool IsServiceStore => ServiceStoreNames.Contains(Name);

    private bool HasAlignment(string alignment) => string.Equals(Alignment, alignment, StringComparison.OrdinalIgnoreCase);
}
