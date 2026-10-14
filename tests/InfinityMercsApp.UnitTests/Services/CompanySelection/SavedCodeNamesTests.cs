using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection;

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
