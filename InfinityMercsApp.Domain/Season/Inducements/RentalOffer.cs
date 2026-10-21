using System.Globalization;
using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Season.Inducements;

/// <summary>Gear the Rented Equipment table offers, and the slot it fills.</summary>
/// <param name="Ava">How many can be rented for one contract; null for T (any number).</param>
public sealed record RentalOffer(string Name, GearSlot Slot, int? Ava, int Cost)
{
    /// <summary>"T", or the number allowed.</summary>
    public string AvaText => Ava?.ToString(CultureInfo.InvariantCulture) ?? "T";
}
