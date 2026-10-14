namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// Which id list on a profile or option an entry comes from (<c>chars</c>, <c>skills</c>, <c>weapons</c>...).
/// </summary>
public enum ProfileEntryKind
{
    Chars,
    Skills,
    Equip,
    Weapons,
    Peripheral,
    Ammunition,
    Ammo
}
