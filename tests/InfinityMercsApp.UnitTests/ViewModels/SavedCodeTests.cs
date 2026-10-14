using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class SavedCodeNamesTests
{
    private static ArmyFilterCatalog Filters => Army101Data.Definition.Filters;

    private static CompanySavedCodeRef Code(int id, params IEnumerable<int> extras) =>
        new() { Id = id, Extra = extras.Any() ? extras.ToList() : null };

    [Fact]
    public void ResolvesNamesWithExtrasAndSkipsUnknownCodes() =>
        Assert.Equal(
            ["Combi Rifle", "Combi Rifle (+1, -3)"],
            SavedCodeNames.Resolve(Filters, [Code(33), Code(33, 298, 6, 6), Code(999_999), null], ProfileEntryKind.Weapons));

    [Fact]
    public void AnEntryListsItsCodedNamesThenItsCustomNames()
    {
        var entry = new SavedCompanyEntry { CurrentSkillCodes = [Code(86)], CustomSkills = ["Homebrew Skill"] };

        Assert.Equal(["No Wound Incapacitation", "Homebrew Skill"], SavedCodeNames.ForEntry(Filters, entry, ProfileEntryKind.Skills));
        Assert.Empty(SavedCodeNames.ForEntry(Filters, entry, ProfileEntryKind.Equip));
    }

    [Fact]
    public void WeaponsSplitIntoRangedAndMeleeWithoutRepeats()
    {
        var entry = new SavedCompanyEntry { CurrentWeaponCodes = [Code(33), Code(6)], CustomWeapons = ["combi rifle", "-", " Knife "] };

        var weapons = SavedCodeNames.Weapons(Filters, entry);

        Assert.Equal(["Combi Rifle"], weapons.Ranged);
        Assert.Equal(["AP CC Weapon", "Knife"], weapons.Melee);
    }

    [Fact]
    public void JoinsOneNamePerLineOrADash()
    {
        Assert.Equal($"A{Environment.NewLine}B", SavedCodeNames.JoinOrDash([" A ", "-", "", "a", "B"]));
        Assert.Equal("-", SavedCodeNames.JoinOrDash(["-", " "]));
    }

    [Theory]
    [InlineData(ProfileEntryKind.Chars, "chars")]
    [InlineData(ProfileEntryKind.Equip, "equip")]
    [InlineData(ProfileEntryKind.Ammo, "ammunition")]
    public void EachEntryKindHasAFilterSection(ProfileEntryKind kind, string section) =>
        Assert.Equal(section, ArmyFilterSection.For(kind));
}

public sealed class SavedCodeEncoderTests
{
    private readonly SavedCodeEncoder _encoder = new(Army101Data.Create());

    private CodedNames Encode(params IEnumerable<string> names) =>
        _encoder.Encode(names, ProfileEntryKind.Weapons, [0, Army101Data.PanOceania]);

    [Fact]
    public void CodesKnownNamesIgnoringCaseAndSpacing()
    {
        var coded = Encode("Combi Rifle", " combi   RIFLE ");

        Assert.Equal([33], coded.Codes.Select(c => c.Id));
        Assert.Null(coded.Codes[0].Extra);
        Assert.Empty(coded.CustomNames);
    }

    [Fact]
    public void CodesBracketedExtrasInIdOrder()
    {
        var code = Assert.Single(Encode("Combi Rifle (+1, -3)").Codes);

        Assert.Equal(33, code.Id);
        Assert.Equal([6, 298], code.Extra!);
    }

    [Fact]
    public void KeepsUncodedNamesAsCustomText()
    {
        var coded = Encode("Laser Spoon", "Combi Rifle (+1, Glitter)");

        Assert.Equal(["Laser Spoon", "Combi Rifle (+1, Glitter)"], coded.CustomNames);
        Assert.Equal([298], Assert.Single(coded.Codes).Extra!);
    }

    [Fact]
    public void RoundTripsThroughSavedCodeNames()
    {
        var names = new List<string> { "Combi Rifle (+1)", "AP CC Weapon" };

        var coded = Encode(names);

        Assert.Equal(names, SavedCodeNames.Resolve(Army101Data.Definition.Filters, coded.Codes, ProfileEntryKind.Weapons));
    }
}
