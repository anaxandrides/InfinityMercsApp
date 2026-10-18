using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Season.Armor;
using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.Armor;

/// <summary>Armour from the shipped market tables, put on troopers by the Mercenaries armour rules.</summary>
public sealed class ArmorFittingTests
{
    private static TrooperProfile Trooper(
        string arm = "1",
        string bts = "0",
        string vitality = "1",
        IReadOnlyList<string>? skills = null,
        IReadOnlyList<string>? characteristics = null) =>
        new("10cm-5cm", "13", "12", "10", "13", arm, bts, vitality, "2", skills ?? ["Courage"], characteristics ?? ["Regular", "Cube"]);

    private static WornArmor Wear(TrooperProfile trooper, string type, string armorName, CaptainStatBonuses? bonuses = null) =>
        ArmorFitting.Wear(trooper, type, PackagedArmor.Named(armorName), bonuses ?? CaptainStatBonuses.None);

    [Fact]
    public void Wear_BecomingHeavyInfantry_GivesWoundsSkillHackableAndShockImmunity()
    {
        var worn = Wear(Trooper(), "MI", "Haidao Armor");

        Assert.Equal("HI", worn.UnitTypeCode);
        Assert.Equal(("3", "3"), (worn.Profile.Arm, worn.Profile.Bts));
        Assert.Equal("1", worn.Profile.Vitality);
        Assert.Contains("No Wound Incapacitation", worn.Profile.Skills);
        Assert.Contains(ArmorFitting.ShockImmunity, worn.Profile.Skills);
        Assert.Contains("NCO", worn.Profile.Skills);
        Assert.Contains(ArmorFitting.Hackable, worn.Profile.Characteristics);
    }

    // Heavy Armor's own wounds skill stacks with the two from becoming HI: Dogged, NWI, then +1 VITA.
    [Fact]
    public void Wear_HeavyArmorFromLight_StacksWoundsSkills()
    {
        var worn = Wear(Trooper(), "LI", "Heavy Armor");

        Assert.Equal("2", worn.Profile.Vitality);
        Assert.DoesNotContain("Dogged", worn.Profile.Skills);
        Assert.DoesNotContain("No Wound Incapacitation", worn.Profile.Skills);
    }

    [Fact]
    public void Wear_LeavingHeavyInfantry_TakesThemAwayAndCapsArm()
    {
        var heavy = Trooper(arm: "4", skills: ["No Wound Incapacitation", ArmorFitting.ShockImmunity], characteristics: ["Regular", ArmorFitting.Hackable]);

        var worn = Wear(heavy, "HI", "Mobility Suit");

        Assert.Equal("LI", worn.UnitTypeCode);
        Assert.Equal("2", worn.Profile.Arm);
        Assert.Equal("1", worn.Profile.Vitality);
        Assert.DoesNotContain("No Wound Incapacitation", worn.Profile.Skills);
        Assert.DoesNotContain(ArmorFitting.ShockImmunity, worn.Profile.Skills);
        Assert.DoesNotContain(ArmorFitting.Hackable, worn.Profile.Characteristics);
        Assert.Equal("15cm-10cm", worn.Profile.Move);
        Assert.Contains("Climbing Plus", worn.Profile.Skills);
    }

    [Fact]
    public void Wear_AnyArmour_ReplacesArmAndBts()
    {
        var worn = Wear(Trooper(arm: "3", bts: "6"), "LI", "Nanosuit");

        Assert.Equal(("1", "3"), (worn.Profile.Arm, worn.Profile.Bts));
    }

    // The LI and MI caps only apply when the armour changed the troop type.
    [Fact]
    public void Wear_TypeChanges_CapsArmOnlyThen()
    {
        Assert.Equal("3", Wear(Trooper(arm: "3"), "LI", "Jumpsuit").Profile.Arm);
        Assert.Equal("2", Wear(Trooper(arm: "3"), "MI", "Jumpsuit").Profile.Arm);
        Assert.Equal("3", Wear(Trooper(arm: "5"), "HI", "Stim Suit").Profile.Arm);
    }

    [Fact]
    public void Wear_CaptainBonuses_StayOnTop()
    {
        var captain = new CaptainStatBonuses(0, 0, 1, 0, 2, 3, 0);

        var worn = Wear(Trooper(arm: "3", bts: "3"), "LI", "Power Suit", captain);

        // Power Suit: ARM 2, BTS 6, PH 15.
        Assert.Equal(("4", "9", "16"), (worn.Profile.Arm, worn.Profile.Bts, worn.Profile.Ph));
    }

    [Fact]
    public void Wear_ArmourDoggedOrNwi_AddsThemAsWritten()
    {
        Assert.Equal(["Courage", "Dogged"], Wear(Trooper(), "LI", "Stim Suit").Profile.Skills);
        Assert.Equal(["No Wound Incapacitation"], Wear(Trooper(skills: ["No Wound Incapacitation"]), "LI", "Stim Suit").Profile.Skills);
        Assert.Equal(["No Wound Incapacitation"], Wear(Trooper(skills: ["Dogged"]), "LI", "Nexus Stim Suit").Profile.Skills);
    }

    [Fact]
    public void Wear_SuperheavyArmor_SetsTwoWoundsWithNwi()
    {
        var worn = Wear(Trooper(skills: ["Dogged"]), "LI", "Superheavy Armor", new CaptainStatBonuses(0, 0, 0, 0, 0, 0, 1));

        Assert.Equal(("3", "5", "10cm-10cm"), (worn.Profile.Vitality, worn.Profile.Silhouette, worn.Profile.Move));
        Assert.Equal([ArmorFitting.ShockImmunity, "No Wound Incapacitation"], worn.Profile.Skills);
    }

    [Fact]
    public void Wear_TeseumPlate_IsNotHackableAndBtsCannotRise()
    {
        var worn = Wear(Trooper(), "MI", "Teseum Plate Armor");

        Assert.DoesNotContain(ArmorFitting.Hackable, worn.Profile.Characteristics);
        Assert.Equal("0", worn.Limit(worn.Profile.With(new TrooperChange { Bts = 3 })).Bts);
    }

    [Fact]
    public void Wear_ArmourImprovingHeldSkill_ImprovesIt()
    {
        Assert.Contains("Infiltration (+3)", Wear(Trooper(skills: ["Infiltration"]), "LI", "Oniwaban Suit").Profile.Skills);
        Assert.Contains("Combat Jump (+3)", Wear(Trooper(skills: ["Combat Jump"]), "MI", "Hellcat Dropsuit").Profile.Skills);
        Assert.Contains("Martial Arts L3", Wear(Trooper(skills: ["Martial Arts L2"]), "LI", "Harmonic Combat Exosuit").Profile.Skills);
        Assert.Contains("Martial Arts L1", Wear(Trooper(), "LI", "Harmonic Combat Exosuit").Profile.Skills);
    }

    [Fact]
    public void Wear_ArmourWithEquipment_ListsIt()
    {
        Assert.Equal(["Holomask", "Holoprojector"], Wear(Trooper(), "LI", "Bashi Holo Suit").Equipment);
    }

    // A trooper has one type: one made a REM is a REM.
    [Fact]
    public void Wear_ArmourOfAnotherType_GivesOneType()
    {
        var remote = Wear(Trooper(characteristics: ["Regular"]), "LI", "Black Labs Body Modification");

        Assert.Equal("REM", remote.UnitTypeCode);
        Assert.Contains(ArmorFitting.Hackable, remote.Profile.Characteristics);
    }

    // A REM changes type like anyone else; Remote Presence is its own skill and stays.
    [Fact]
    public void Wear_RemoteInTypedArmour_ChangesTypeAndKeepsRemotePresence()
    {
        var remote = Trooper(skills: ["Remote Presence"], characteristics: ["Regular", ArmorFitting.Hackable]);

        var medium = Wear(remote, "REM", "Stim Suit");
        var heavy = Wear(remote, "REM", "Haidao Armor");

        Assert.Equal("MI", medium.UnitTypeCode);
        Assert.Contains("Remote Presence", medium.Profile.Skills);
        Assert.DoesNotContain(ArmorFitting.Hackable, medium.Profile.Characteristics);
        Assert.Equal("HI", heavy.UnitTypeCode);
        Assert.Contains(ArmorFitting.ShockImmunity, heavy.Profile.Skills);
        Assert.Contains(ArmorFitting.Hackable, heavy.Profile.Characteristics);
    }

    [Fact]
    public void Wear_Tag_ChangesNothing()
    {
        var tag = Trooper(arm: "8");

        var worn = Wear(tag, "TAG", "Heavy Armor");

        Assert.Equal(("TAG", tag), (worn.UnitTypeCode, worn.Profile));
    }

    [Fact]
    public void Wear_NoArmour_ChangesNothing()
    {
        var trooper = Trooper();

        Assert.Equal(trooper, ArmorFitting.Wear(trooper, "LI", null, CaptainStatBonuses.None).Profile);
    }

    [Fact]
    public void Effects_ChemtankAndWetsuit_RollWhenBought()
    {
        Assert.Equal(RecruitRollTable.Metachemistry, PackagedArmor.Named("Chemtank").Effects.Roll);
        Assert.Equal(RecruitRollTable.Booty, PackagedArmor.Named("Irmandihno Wetsuit").Effects.Roll);
        Assert.Equal(2, PackagedArmor.All.Count(armor => armor.Effects.Roll is not null));
    }

    // Belts and Pouches, Chemtank's Metachemistry, the Wetsuit's Booty and Symbio-Resonance's profile choice are
    // handled elsewhere; every other row must say what it does.
    [Fact]
    public void All_EveryShippedArmour_HasItsEffects()
    {
        IReadOnlyList<string> elsewhere = ["Symbio - Resonance"];

        var blank = PackagedArmor.All
            .Where(armor => !elsewhere.Contains(armor.ArmorName))
            .Where(armor => armor.Type is null && armor.Arm is null && armor.Bts is null &&
                            armor.Effects is { ExtraSlot: false, Skills.Count: 0, Equipment.Count: 0, WoundsSkill: 0, MartialArtsLevels: 0, Move: null })
            .Select(armor => armor.ArmorName);

        Assert.Empty(blank);
    }
}
