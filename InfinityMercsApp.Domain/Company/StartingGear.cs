using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.SpecOps;

namespace InfinityMercsApp.Domain.Company;

/// <summary>
/// One-off credits a trooper spends as it joins, on the Base Market and its faction's War Market: what it buys becomes
/// part of its own profile, so it takes no gear slot, and whatever is left over is lost. A Proxy Pack's Lone Wolf has
/// 10 in place of the Spec-Ops XP it used to get.
/// </summary>
public static partial class StartingGear
{
    /// <summary>What a Proxy Pack's Lone Wolf has to spend.</summary>
    public const int ProxyPackCredits = 10;

    /// <summary>Where the gear comes from, as its purchase line reads: "Starting gear: Combi Rifle".</summary>
    public const string Source = "Starting gear";

    private static readonly IReadOnlyList<GearSlot> Offered =
        [GearSlot.Primary, GearSlot.Secondary, GearSlot.Sidearm, GearSlot.Accessories, GearSlot.Roles];

    /// <summary>
    /// Whether an item can be bought this way: weapons, accessories and roles. Armour and augments change a trooper
    /// beyond its profile's lines, and an SWC exchange isn't gear.
    /// </summary>
    public static bool IsOffered(StoreItem item) =>
        GearSlots.TryParse(item.Category, out var slot) && Offered.Contains(slot);

    /// <summary>
    /// The item as part of the trooper's profile: a weapon (any Primary, Secondary or Sidearm, or an accessory that is
    /// one of <paramref name="weaponNames"/>) or a piece of equipment; a role is its skill and what comes with it,
    /// "Doctor (+Medikit)" being Doctor and a Medikit.
    /// </summary>
    public static TrooperUpgrade UpgradeFor(StoreItem item, IReadOnlySet<string> weaponNames)
    {
        var name = item.Name.Trim();
        List<string> weapons = [];
        List<string> skills = [];
        List<string> equipment = [];
        void AddItem(string itemName) => (weaponNames.Contains(itemName) ? weapons : equipment).Add(itemName);

        GearSlots.TryParse(item.Category, out var slot);
        switch (slot)
        {
            case GearSlot.Roles when RoleWithItem().Match(name) is { Success: true } role:
                skills.Add(role.Groups["skill"].Value.Trim());
                AddItem(role.Groups["item"].Value.Trim());
                break;
            case GearSlot.Roles:
                skills.Add(name);
                break;
            case GearSlot.Accessories:
                AddItem(name);
                break;
            default:
                weapons.Add(name);
                break;
        }

        return new TrooperUpgrade(name, CaptainStatBonuses.None, null, null, weapons, skills, equipment, Source);
    }

    [GeneratedRegex(@"^(?<skill>[^(]+)\(\+(?<item>[^)]+)\)\s*$")]
    private static partial Regex RoleWithItem();
}
