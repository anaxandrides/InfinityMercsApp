using InfinityMercsApp.Domain.Models.Army.Stats;

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

    /// <summary>The first two movement values, when the profile has at least two.</summary>
    public Movement? Movement => MoveCm.Count >= 2 ? new Movement(MoveCm[0], MoveCm[1]) : null;

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

    /// <summary>A copy of this profile with a different AVA.</summary>
    public UnitProfile WithAva(int? ava) => new()
    {
        Id = Id,
        Name = Name,
        Logo = Logo,
        Type = Type,
        Str = Str,
        Move = Move,
        MoveCm = MoveCm,
        Cc = Cc,
        Bs = Bs,
        Ph = Ph,
        Wip = Wip,
        Arm = Arm,
        Bts = Bts,
        W = W,
        S = S,
        Ava = ava,
        StrValue = StrValue,
        Vita = Vita,
        HasStatFields = HasStatFields,
        Chars = Chars,
        Skills = Skills,
        Equip = Equip,
        Weapons = Weapons,
        Peripheral = Peripheral,
        Ammunition = Ammunition,
        Ammo = Ammo
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
    /// The wounds column: <see cref="Stat.Str"/> with a numeric structure value, otherwise <see cref="Stat.Vita"/>
    /// with W (or VITA).
    /// </summary>
    public StatValue Vitality
    {
        get
        {
            if (StrValue is int structure)
            {
                return new StatValue(Stat.Str, structure.ToString());
            }

            return new StatValue(Stat.Vita, W is int wounds ? wounds.ToString() : StatText(Vita));
        }
    }
}
