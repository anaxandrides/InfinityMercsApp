using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.UnitTests.Company;

public sealed class StartingGearTests
{
    private static readonly IReadOnlySet<string> Weapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Flash Pulse", "Combi Rifle" };

    private static StoreItem Item(string name, string category, int cost = 5) => new() { Name = name, Category = category, CostCr = cost };

    [Theory]
    [InlineData("Primary", true)]
    [InlineData("Secondary", true)]
    [InlineData("Sidearm", true)]
    [InlineData("Accessories", true)]
    [InlineData("Roles", true)]
    [InlineData("Armor", false)]
    [InlineData("Augments", false)]
    [InlineData("Exchange", false)]
    public void IsOffered_Category_OffersOnlyWhatFitsAProfile(string category, bool offered) =>
        Assert.Equal(offered, StartingGear.IsOffered(Item("Thing", category)));

    [Fact]
    public void UpgradeFor_Weapon_AddsTheWeapon()
    {
        var upgrade = StartingGear.UpgradeFor(Item("Combi Rifle", "Primary"), Weapons);

        Assert.Equal(["Combi Rifle"], upgrade.Weapons);
        Assert.Equal("Starting gear: Combi Rifle", upgrade.PurchaseLine);
    }

    [Theory]
    [InlineData("Albedo (-3)", false)]
    [InlineData("Flash Pulse", true)]
    public void UpgradeFor_Accessory_IsAWeaponOnlyWhenOneIsNamedSo(string name, bool isWeapon)
    {
        var upgrade = StartingGear.UpgradeFor(Item(name, "Accessories"), Weapons);

        Assert.Equal(isWeapon ? [name] : [], upgrade.Weapons);
        Assert.Equal(isWeapon ? [] : [name], upgrade.Equipment);
    }

    [Fact]
    public void UpgradeFor_RoleWithEquipment_IsTheSkillAndTheEquipment()
    {
        var upgrade = StartingGear.UpgradeFor(Item("Doctor (+Medikit)", "Roles", 10), Weapons);

        Assert.Equal(["Doctor"], upgrade.Skills);
        Assert.Equal(["Medikit"], upgrade.Equipment);
    }

    [Fact]
    public void UpgradeFor_RoleWithWeapon_IsTheSkillAndTheWeapon()
    {
        var upgrade = StartingGear.UpgradeFor(Item("Forward Observer (+Flash Pulse)", "Roles", 8), Weapons);

        Assert.Equal(["Forward Observer"], upgrade.Skills);
        Assert.Equal(["Flash Pulse"], upgrade.Weapons);
    }
}
