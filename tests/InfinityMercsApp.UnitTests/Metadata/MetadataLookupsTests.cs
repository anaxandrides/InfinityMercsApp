using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Metadata;

public sealed class MetadataLookupsTests
{
    private readonly IMetadataProvider _metadata = Substitute.For<IMetadataProvider>();

    [Fact]
    public void PrefersWeaponsNamedExactlyAsTheItem()
    {
        _metadata.SearchWeaponsByName("Combi Rifle").Returns(
        [
            new Weapon { Name = "Combi Rifle", Mode = "Shock" },
            new Weapon { Name = "Combi Rifle Plus" },
            new Weapon { Name = "combi rifle", Mode = "Normal" }
        ]);

        var weapons = _metadata.WeaponsFor("Combi Rifle (+1B)");

        Assert.Equal<string?>(["Shock", "Normal"], weapons.Select(weapon => weapon.Mode));
    }

    [Fact]
    public void FallsBackToEveryMatch()
    {
        _metadata.SearchWeaponsByName("Rifle").Returns([new Weapon { Name = "Combi Rifle" }, new Weapon { Name = "MULTI Rifle" }]);

        Assert.Equal(["Combi Rifle", "MULTI Rifle"], _metadata.WeaponsFor("Rifle").Select(weapon => weapon.Name));
    }
}
