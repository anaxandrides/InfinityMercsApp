namespace InfinityMercsApp.Domain.Models.Stores;

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
