namespace InfinityMercsApp.Services.Season.Peripherals;

/// <summary>
/// A Tohaa peripheral the Peripheral (Controller) augment can add: one option of a peripheral profile group in the
/// Tohaa army list, at that option's cost, with the skills, equipment and weapons the option gives it.
/// </summary>
public sealed record PeripheralOffer(int UnitId, string UnitName, int GroupId, int OptionId, string Name, int Cost, string Loadout = "")
{
    /// <summary>E.g. "Chaksa Peripheral B: Pulzar, Pistol (Gorgos Squad), 4 CR".</summary>
    public string Display => Loadout.Length == 0 ? $"{Name} ({UnitName}), {Cost} CR" : $"{Name}: {Loadout} ({UnitName}), {Cost} CR";
}
