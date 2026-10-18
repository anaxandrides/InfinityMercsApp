using InfinityMercsApp.Services.CompanySelection.Profiles;
using InfinityMercsApp.UnitTests.Services.CompanySelection.Profiles;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection.Items;

public sealed class NewTrooperEntryTests
{
    private static readonly ArmyUnitSelectionItem SuJian = new() { Id = 155, SourceFactionId = 201, Name = "Sù-Jiàn", Subtitle = "HI - Elite Troops" };

    private static readonly ViewerProfileItem Profile = new() { Name = "SÙ-JIÀN", Cost = "35", GroupId = 1 };

    private static CompanyUnitDetail Detail()
    {
        var detail = new CompanyUnitDetail { ShowUnitsInInches = false };
        detail.ApplyFirstProfileStats(TransmutationFormsTests.SuJian);
        return detail;
    }

    [Fact]
    public void From_NoForm_UsesTheFirstProfilesStatline() =>
        Assert.StartsWith("MOV 20-5 | CC 9", NewTrooperEntry.From(SuJian, Profile, Detail(), ["Transmutation (Auto)"], null).Subtitle);

    [Fact]
    public void From_FormChosen_TakesItsStatlineAndSkillsWithoutTransmutation()
    {
        var form = TransmutationForms.Of(TransmutationFormsTests.SuJian.Groups[0].Profiles[1], TransmutationFormsTests.Filters, showUnitsInInches: false);

        var entry = NewTrooperEntry.From(SuJian, Profile, Detail(), ["Courage", "Transmutation (Auto)"], null, form);

        Assert.StartsWith("MOV 10-10 | CC 17", entry.Subtitle);
        Assert.Equal(10, entry.UnitMoveFirstCm);
        Assert.Equal(10, entry.UnitMoveSecondCm);
        Assert.Equal(string.Join(Environment.NewLine, "Courage", "BS Attack"), entry.SavedSkills);
    }
}
