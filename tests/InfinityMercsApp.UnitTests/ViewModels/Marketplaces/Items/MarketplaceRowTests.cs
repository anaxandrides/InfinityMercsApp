using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.ViewModels.Marketplaces.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Marketplaces.Items;

public sealed class MarketplaceRowTests
{
    [Fact]
    public void Quantity_Negative_StaysAtZero()
    {
        var row = new AugmentRow(new StoreAugment { Name = "Cube" }) { Quantity = 2 };

        row.Quantity = -1;

        Assert.Equal(0, row.Quantity);
    }

    [Fact]
    public void CostDisplay_StoreItem_ShowsCreditsAndSwc()
    {
        var row = new StoreItemRow(new StoreItem { Name = "Pistol", CostCr = 3, CostSwc = 0.5m });

        Assert.Equal("3cr / 0.5swc", row.CostDisplay);
        Assert.Equal("Pistol", row.CartName);
    }

    [Fact]
    public void DisplayName_TroopType_NamesArmourAndType()
    {
        var row = new TroopTypeRow(new StoreTroopType { ArmorName = "Light Armour", Type = "LI", CostCr = 10, Arm = 1 });

        Assert.Equal("Light Armour (LI)", row.DisplayName);
        Assert.Equal("Light Armour (LI)", row.CartName);
        Assert.Equal("ARM 1  |  BTS —", row.StatDisplay);
        Assert.Equal("10cr", row.CostDisplay);
        Assert.False(row.HasAbilities);
    }

    [Fact]
    public void DisplayName_TroopTypeWithoutType_IsJustTheArmour() =>
        Assert.Equal("Heavy Armour", new TroopTypeRow(new StoreTroopType { ArmorName = "Heavy Armour" }).DisplayName);

    [Fact]
    public void RequirementDisplay_NoRequirement_ShowsADash()
    {
        var row = new AugmentRow(new StoreAugment { Name = "Cube", CostCr = 4 });

        Assert.False(row.HasRequirement);
        Assert.Equal("—", row.RequirementDisplay);
        Assert.Equal("4cr", row.CostDisplay);
    }

    [Fact]
    public void StoreItemGroupFrom_Items_GroupsByCategoryInStoreOrder()
    {
        var groups = StoreItemGroup.From(
        [
            new StoreItem { Name = "Pistol", Category = "Weapons" },
            new StoreItem { Name = "Medkit", Category = "Gear" },
            new StoreItem { Name = "Rifle", Category = "Weapons" }
        ]);

        Assert.Equal(["Weapons", "Gear"], groups.Select(group => group.Category));
        Assert.Equal(["Pistol", "Rifle"], groups[0].Items.Select(item => item.Name));
    }

    [Fact]
    public void MarketplaceCartLineFrom_Rows_KeepsThoseWithAQuantity()
    {
        IEnumerable<MarketplaceRow> rows =
        [
            new StoreItemRow(new StoreItem { Name = "Pistol", CostCr = 3 }) { Quantity = 2 },
            new AugmentRow(new StoreAugment { Name = "Cube", CostCr = 4 })
        ];

        var lines = MarketplaceCartLine.From(rows);

        Assert.Equal<MarketplaceCartLine>([new("Pistol", 2, 6)], lines);
        Assert.Equal("x2", lines[0].QuantityDisplay);
        Assert.Equal("6cr", lines[0].CostDisplay);
    }
}
