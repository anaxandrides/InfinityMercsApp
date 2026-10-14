namespace InfinityMercsApp.Domain.Models.Stores;

/// <summary>The stores that are services rather than markets; they are always offered, after the markets.</summary>
public static class ServiceStoreNames
{
    public const string MedicalServices = "Medical Services";
    public const string AdditionalRecruitment = "Additional Recruitment";

    public static bool Contains(string? storeName) =>
        string.Equals(storeName, MedicalServices, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(storeName, AdditionalRecruitment, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Store alignments as written in the store files.</summary>
public static class StoreAlignments
{
    /// <summary>Prices rise with the company's notoriety.</summary>
    public const string Lawful = "Lawful";

    /// <summary>Prices fall with the company's notoriety.</summary>
    public const string Chaotic = "Chaotic";

    public const string Neutral = "Neutral";
    public const string AlwaysAvailable = "Always Available";
}

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
