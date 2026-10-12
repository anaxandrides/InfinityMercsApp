namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// A trooper statline (the API's <c>profileGroups[].profiles[]</c>).
/// Stats are <c>null</c> when the payload has no usable integer for them; use <see cref="StatText"/> for display.
/// </summary>
public sealed class UnitProfile
{
    public int? Id { get; init; }

    public string? Name { get; init; }

    public string? Logo { get; init; }

    public int? Type { get; init; }

    public bool Str { get; init; }

    /// <summary>Movement as display text, e.g. <c>"10-5"</c>, or <c>"-"</c>.</summary>
    public string Move { get; init; } = "-";

    /// <summary>
    /// Movement values in centimetres as sent (<c>[10, 5]</c>), read from a <c>move</c>/<c>mov</c> array or
    /// a <c>"10-5"</c> string. Empty when absent.
    /// </summary>
    public IReadOnlyList<int> MoveCm { get; init; } = [];

    /// <summary>First and second movement values in cm, when the profile has at least two.</summary>
    public (int? FirstCm, int? SecondCm) MoveFirstSecondCm =>
        MoveCm.Count >= 2 ? (MoveCm[0], MoveCm[1]) : (null, null);

    public int? Cc { get; init; }

    public int? Bs { get; init; }

    public int? Ph { get; init; }

    public int? Wip { get; init; }

    public int? Arm { get; init; }

    public int? Bts { get; init; }

    public int? W { get; init; }

    public int? S { get; init; }

    public int? Ava { get; init; }

    /// <summary>A numeric structure value (the API normally sends <c>str</c> as a true/false flag instead).</summary>
    public int? StrValue { get; init; }

    public int? Vita { get; init; }

    /// <summary>True when the payload carries any statline field (cc, bs, ph, wip, arm, bts, w or str).</summary>
    public bool HasStatFields { get; init; }

    public IReadOnlyList<ProfileEntry> Chars { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Skills { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Equip { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Weapons { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Peripheral { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Ammunition { get; init; } = [];

    public IReadOnlyList<ProfileEntry> Ammo { get; init; } = [];

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

    /// <summary>A stat as statline text: the number, or <c>"-"</c> when absent.</summary>
    public static string StatText(int? value) => value?.ToString() ?? "-";

    /// <summary>AVA as statline text: <c>"T"</c> for total (255), <c>"-"</c> when absent or negative.</summary>
    public string AvaText => Ava switch
    {
        null => "-",
        < 0 => "-",
        255 => "T",
        _ => Ava.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// The wounds column: <c>STR</c> with a numeric structure value, otherwise <c>VITA</c> with W (or VITA).
    /// </summary>
    public (string Header, string Value) Vitality
    {
        get
        {
            if (StrValue is int structure)
            {
                return ("STR", structure.ToString());
            }

            return W is int wounds ? ("VITA", wounds.ToString()) : ("VITA", StatText(Vita));
        }
    }
}

/// <summary>
/// A group of related profiles and the options that field them (the API's <c>profileGroups[]</c>).
/// </summary>
public sealed class ProfileGroup
{
    public int? Id { get; init; }

    public string? Isc { get; init; }

    public int? Category { get; init; }

    public IReadOnlyList<UnitProfile> Profiles { get; init; } = [];

    public IReadOnlyList<ProfileOption> Options { get; init; } = [];

    /// <summary>
    /// Peripheral entries declared on the group's own profiles (a controller's peripherals).
    /// </summary>
    public IReadOnlyList<ProfileEntry> ControllerPeripheralEntries =>
        Profiles.SelectMany(profile => profile.Peripheral).ToList();
}
