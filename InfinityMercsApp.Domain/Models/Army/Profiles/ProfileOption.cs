namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// One purchasable loadout of a profile group (the API's <c>profileGroups[].options[]</c>).
/// </summary>
public sealed class ProfileOption
{
    private const string LieutenantToken = "lieutenant";

    public int? Id { get; init; }

    /// <summary>The option name, or <c>null</c> when the payload has none.</summary>
    public string? Name { get; init; }

    /// <summary>Support-weapon cost as display text (<c>"0"</c>, <c>"1.5"</c>, <c>"+1"</c>), <c>"-"</c> when absent.</summary>
    public string Swc { get; set; } = "-";

    /// <summary>Point cost as display text, <c>"-"</c> when absent. Taken from <c>points</c>, then <c>cost</c>, then <c>pts</c>.</summary>
    public string Cost { get; set; } = "-";

    /// <summary>Number of miniatures the option fields; never negative.</summary>
    public int Minis { get; init; }

    public bool Disabled { get; init; }

    public IReadOnlyList<OrderEntry> Orders { get; set; } = [];

    public IReadOnlyList<IncludeReference> Includes { get; set; } = [];

    public IReadOnlyList<ProfileEntry> Chars { get; set; } = [];

    public IReadOnlyList<ProfileEntry> Skills { get; set; } = [];

    public IReadOnlyList<ProfileEntry> Equip { get; set; } = [];

    public IReadOnlyList<ProfileEntry> Weapons { get; set; } = [];

    public IReadOnlyList<ProfileEntry> Peripheral { get; set; } = [];

    public IReadOnlyList<ProfileEntry> Ammunition { get; set; } = [];

    public IReadOnlyList<ProfileEntry> Ammo { get; set; } = [];

    /// <summary>The option's cost as a number (see <see cref="ProfileValues.ParseCost"/>).</summary>
    public int CostValue => ProfileValues.ParseCost(Cost);

    /// <summary>The option costs SWC, which a mercenary profile may not; one that gives SWC ("+1") does not.</summary>
    public bool CostsSwc => ProfileValues.CostsSwc(Swc);

    public bool HasLieutenantOrder => Orders.Any(order => order.IsLieutenant);

    /// <summary>The option's own entries of one kind (without following includes).</summary>
    public IReadOnlyList<ProfileEntry> Entries(ProfileEntryKind kind) => kind switch
    {
        ProfileEntryKind.Chars => Chars,
        ProfileEntryKind.Skills => Skills,
        ProfileEntryKind.Equip => Equip,
        ProfileEntryKind.Weapons => Weapons,
        ProfileEntryKind.Peripheral => Peripheral,
        ProfileEntryKind.Ammunition => Ammunition,
        ProfileEntryKind.Ammo => Ammo,
        _ => []
    };

    /// <summary>
    /// True when the option makes the trooper a Lieutenant: a LIEUTENANT order, "lieutenant" in the
    /// option name, or a skill whose looked-up name contains "lieutenant".
    /// </summary>
    public bool IsLieutenant(IReadOnlyDictionary<int, string> skillNames)
    {
        if (HasLieutenantOrder)
        {
            return true;
        }

        if (Name?.Contains(LieutenantToken, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        foreach (var skill in Skills)
        {
            if (skill.Id is int id &&
                skillNames.TryGetValue(id, out var name) &&
                name.Contains(LieutenantToken, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
