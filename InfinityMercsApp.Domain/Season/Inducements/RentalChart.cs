using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Season.Inducements;

/// <summary>
/// The Rented Equipment table. Names are the markets' and the army data's, so the gear works like bought gear: HMG
/// is the Heavy Machine Gun, MSV1 the Multispectral Visor Level 1. The table's Equipment rows fill Accessories.
/// </summary>
public static class RentalChart
{
    public static IReadOnlyList<RentalOffer> Offers { get; } =
    [
        new("AP Marksman Rifle", GearSlot.Primary, null, 5),
        new("Breaker Combi Rifle", GearSlot.Primary, null, 5),
        new("Spitfire", GearSlot.Primary, 2, 10),
        new("MULTI Sniper Rifle", GearSlot.Primary, 1, 15),
        new("Heavy Machine Gun", GearSlot.Primary, 1, 15),
        new("Panzerfaust", GearSlot.Secondary, null, 5),
        new("Flammenspeer", GearSlot.Secondary, null, 5),
        new("Blitzen", GearSlot.Secondary, null, 5),
        new("Symbiobomb", GearSlot.Secondary, 1, 10),
        new("Symbiomate", GearSlot.Secondary, 1, 10),
        new("Tinbot: Firewall (-3)", GearSlot.Accessories, 2, 5),
        new("Multispectral Visor Level 1", GearSlot.Accessories, 2, 5),
        new("Albedo (-3)", GearSlot.Accessories, 2, 5),
        new("Tinbot: Firewall (-6)", GearSlot.Accessories, 1, 10),
        new("Multispectral Visor Level 2", GearSlot.Accessories, 1, 10),
        new("Albedo (-6)", GearSlot.Accessories, 1, 10)
    ];
}
