using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.Season.Peripherals;

namespace InfinityMercsApp.UnitTests.Services.Season.Peripherals;

public sealed class TohaaPeripheralsTests
{
    private readonly TohaaPeripherals _peripherals = new(TohaaArmyData.Create());

    // Servants and mercenaries' peripherals are left out.
    [Fact]
    public void OffersEachSynchronizedPeripheralAtItsOptionsCost()
    {
        Assert.Equal(
        [
            new PeripheralOffer(TohaaArmyData.GorgosId, "Gorgos Squad", 2, 1, "Chaksa Peripheral A", 7, "Combi Rifle"),
            new PeripheralOffer(TohaaArmyData.GorgosId, "Gorgos Squad", 2, 2, "Chaksa Peripheral B", 4, "Combi Rifle")
        ], _peripherals.Offers());
        Assert.Equal("Chaksa Peripheral B: Combi Rifle (Gorgos Squad), 4 CR", _peripherals.Offers()[1].Display);
    }

    [Fact]
    public void ABoughtPeripheralJoinsTheRosterConnectedToItsController()
    {
        var entry = _peripherals.EntryFor(_peripherals.Offers()[1], entryIndex: 9, controllerEntryIndex: 2);

        Assert.NotNull(entry);
        Assert.Equal((9, 2, 4), (entry.EntryIndex, entry.ParentEntryIndex, entry.Cost));
        Assert.True(entry.IsPeripheralUnit && entry.FromControllerAugment);
        Assert.Equal("Chaksa Peripheral B", entry.Name);
        Assert.Equal(UnitTypeCodes.LightInfantry, entry.UnitTypeCode);
        Assert.Equal(("17", "11", "1", "2"), (entry.CurrentCc, entry.CurrentBs, entry.CurrentVitaOrStr, entry.CurrentS));
        Assert.Equal((15, 10), (entry.CurrentMoveFirstCm, entry.CurrentMoveSecondCm));
        Assert.Equal([TohaaArmyData.CombiRifleId], entry.CurrentWeaponCodes.Select(code => code.Id));
        Assert.Equal([TohaaArmyData.SynchronizedId], entry.CurrentSkillCodes.Single().Extra);
    }

    [Fact]
    public void APeripheralNoLongerInTheDataGivesNoEntry() =>
        Assert.Null(_peripherals.EntryFor(new PeripheralOffer(1, "Gone", 2, 1, "Gone", 3), 9, 2));
}
