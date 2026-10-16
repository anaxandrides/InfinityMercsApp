using InfinityMercsApp.Services.CompanyProfiles;

namespace InfinityMercsApp.Services.CompanySelection.Models.Saved;

/// <summary>A unit's weapons split into ranged and close-combat lists.</summary>
public sealed record WeaponLists(IReadOnlyList<string> Ranged, IReadOnlyList<string> Melee)
{
    public static WeaponLists Split(IEnumerable<string> weapons)
    {
        var list = weapons.ToList();
        return new WeaponLists(
            list.Where(name => !CompanyProfileText.IsMeleeWeaponName(name)).ToList(),
            list.Where(CompanyProfileText.IsMeleeWeaponName).ToList());
    }
}
