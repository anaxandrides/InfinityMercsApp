namespace InfinityMercsApp.Domain.Models.Season.Gear;

/// <summary>The slots a trooper's season gear goes in; each also names the inventory category it is filled from.</summary>
public enum GearSlot
{
    Primary,
    Secondary,
    Sidearm,
    Accessories,
    Roles,
    Armor,
    Augments,

    /// <summary>Belts and Pouches' one more slot, for gear of any kind but armour.</summary>
    Extra
}
