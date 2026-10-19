using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Services.Season.Inducements;

/// <summary>A hired troop's army profile in the loadout it was hired in, with what the hire adds.</summary>
/// <param name="Vitality">VITA, or STR for a TAG or remote.</param>
public sealed record HiredTrooperProfile(
    string Move,
    string Cc,
    string Bs,
    string Ph,
    string Wip,
    string Arm,
    string Bts,
    StatValue Vitality,
    string S,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Equipment,
    IReadOnlyList<string> RangedWeapons,
    IReadOnlyList<string> MeleeWeapons,
    bool IsIrregular,
    bool IsImpetuous);
