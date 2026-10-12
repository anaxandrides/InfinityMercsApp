using System.Globalization;

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
    public string Swc { get; init; } = "-";

    /// <summary>Point cost as display text, <c>"-"</c> when absent. Taken from <c>points</c>, then <c>cost</c>, then <c>pts</c>.</summary>
    public string Cost { get; init; } = "-";

    /// <summary>Number of miniatures the option fields; never negative.</summary>
    public int Minis { get; init; }

    public bool Disabled { get; init; }

    public IReadOnlyList<OrderEntry> Orders { get; init; } = [];

    public IReadOnlyList<IncludeReference> Includes { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Chars { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Skills { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Equip { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Weapons { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Peripheral { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Ammunition { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Ammo { get; init; } = [];

    /// <summary>The option's cost as a number (see <see cref="ProfileValues.ParseCost"/>).</summary>
    public int CostValue => ProfileValues.ParseCost(Cost);

    public bool HasPositiveSwc => ProfileValues.IsPositiveSwc(Swc);

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

/// <summary>
/// Shared interpretation of the API's loosely typed cost values.
/// </summary>
public static class ProfileValues
{
    /// <summary>
    /// Reads a cost such as <c>"25"</c> or <c>"x7"</c> as a number: the whole text when it is an integer,
    /// otherwise its first run of digits, otherwise 0.
    /// </summary>
    public static int ParseCost(string? cost)
    {
        if (string.IsNullOrWhiteSpace(cost))
        {
            return 0;
        }

        if (int.TryParse(cost, out var parsed))
        {
            return parsed;
        }

        var match = System.Text.RegularExpressions.Regex.Match(cost, "\\d+");
        return match.Success && int.TryParse(match.Value, out var fallback) ? fallback : 0;
    }

    /// <summary>True when an SWC text such as <c>"0.5"</c> or <c>"+1"</c> is greater than zero.</summary>
    public static bool IsPositiveSwc(string? swc)
    {
        if (string.IsNullOrWhiteSpace(swc) || swc == "-")
        {
            return false;
        }

        return decimal.TryParse(swc, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value > 0m;
    }
}
