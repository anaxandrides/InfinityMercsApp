namespace InfinityMercsApp.Domain.Models.Metadata;

public class Weapon
{
    public string WeaponKey { get; set; } = string.Empty;

    public int WeaponId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Type { get; set; }

    public string? Mode { get; set; }

    public string? Wiki { get; set; }

    public int? AmmunitionId { get; set; }

    public string? Burst { get; set; }

    public string? Damage { get; set; }

    public string? Saving { get; set; }

    public string? SavingNum { get; set; }

    public string? Profile { get; set; }

    public IReadOnlyList<string> Properties { get; set; } = [];

    /// <summary>The weapon's range bands; <c>null</c> when it has none (e.g. CC weapons).</summary>
    public WeaponDistance? Distance { get; set; }
}
