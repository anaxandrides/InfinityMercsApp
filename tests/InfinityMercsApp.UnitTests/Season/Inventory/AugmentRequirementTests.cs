using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.UnitTests.Season.Inventory;

public sealed class AugmentRequirementTests
{
    public static TheoryData<string, IReadOnlyList<string>, bool> Requirements => new()
    {
        { "Engineer", ["Courage", "Engineer"], true },
        { "Engineer", ["Courage"], false },
        { "Doctor", ["Doctor (+3)"], true },
        { "Engineer", ["Engineer (+Gizmokit)"], true },
        { "Hacker", ["ECM: Hacker"], false },
        { "Hacker", ["Hacker (+Killer Hacking Device)"], true },
        { "Killer/Hacking Device", ["Killer Hacking Device"], true },
        { "Killer/Hacking Device", ["Hacking Device Plus"], true },
        { "Killer/Hacking Device", ["Hacker (+Killer Hacking Device)"], true },
        { "Killer/Hacking Device", ["Hacker"], false },
        { "Forward Observer", ["Forward Deployment"], false },
        { "Cannot have the BS Attack (+#) skill", ["BS Attack (+1B)"], false },
        { "Cannot have the BS Attack (+#) skill", ["BS Attack (SR-1)"], true },
        { "Cannot have the BS Attack (+#) skill", [], true }
    };

    [Theory]
    [MemberData(nameof(Requirements))]
    public void IsMetBy_HeldItems_MeetsTheRequirement(string requirement, IReadOnlyList<string> held, bool met) =>
        Assert.Equal(met, AugmentRequirement.From(requirement)?.IsMetBy(held));

    [Theory]
    [InlineData(null)]
    [InlineData("-")]
    [InlineData(" ")]
    public void From_NoRequirement_ReturnsNull(string? text) => Assert.Null(AugmentRequirement.From(text));

    [Theory]
    [InlineData("Engineer", "Needs Engineer")]
    [InlineData("Cannot have the BS Attack (+#) skill", "Cannot have the BS Attack (+#) skill")]
    public void Unmet_Requirement_SaysWhatIsNeeded(string requirement, string unmet) => Assert.Equal(unmet, new AugmentRequirement(requirement).Unmet);

    [Fact]
    public void Find_AugmentWithRequirement_ReturnsTheRequirement()
    {
        var catalog = InventoryCatalog.From([new Store
        {
            Name = "Number One",
            Augments = [new StoreAugment { Name = "Armed Turret (MMR)", Requirement = "Engineer", CostCr = 10 }]
        }]);

        Assert.Equal(new AugmentRequirement("Engineer"), catalog.Find("Armed Turret (MMR)")?.Requirement);
    }
}
