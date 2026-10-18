using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Presentation.Units;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Components;

namespace InfinityMercsApp.UnitTests.ViewModels.Components;

public sealed class UnitDetailViewModelTests
{
    private static readonly int SwissGuardId = Army101Data.UnitNamed("SWISS GUARD").Id;

    private static UnitDetailViewModel Load(bool mercsOnly = false, bool lieutenantOnly = false)
    {
        var detail = new UnitDetailViewModel(Army101Data.Create());
        detail.Load(new UnitDetailRequest(Army101Data.PanOceania, SwissGuardId, "Swiss Guard", "logo.svg", mercsOnly, lieutenantOnly));
        return detail;
    }

    [Fact]
    public void Load_Unit_ShowsStatsSummariesAndConfigurations()
    {
        var detail = Load();

        Assert.Equal("Swiss Guard", detail.UnitNameHeading);
        Assert.Equal("logo.svg", detail.LogoSource);
        Assert.NotEqual("-", detail.Stats.Bs);
        Assert.Contains("-", detail.Stats.Mov);
        Assert.NotEmpty(detail.Profiles);
        Assert.Equal($"{detail.Profiles.Count} configurations loaded.", detail.ProfilesStatus);
        Assert.StartsWith("Equipment: ", detail.EquipmentSummary);
        Assert.All(detail.Profiles, p => Assert.StartsWith("SWC ", p.SwcDisplay));
    }

    [Fact]
    public void Load_LieutenantOnly_ListsOnlyLieutenantConfigurations()
    {
        var detail = Load(lieutenantOnly: true);
        Assert.All(detail.Profiles, p => Assert.True(p.IsLieutenant));
    }

    [Fact]
    public void Load_MercsOnly_HidesSwcConfigurations()
    {
        var all = Load();
        var mercs = Load(mercsOnly: true);

        Assert.True(mercs.Profiles.Count < all.Profiles.Count);
        Assert.All(mercs.Profiles, p => Assert.False(ProfileValues.CostsSwc(p.Swc)));
        Assert.All(mercs.Profiles, p => Assert.Equal(string.Empty, p.SwcDisplay));
    }

    [Fact]
    public void Load_MercsOnly_KeepsConfigurationsThatGiveSwc()
    {
        var joan = Army101Data.UnitNamed("JOAN OF ARC");
        var detail = new UnitDetailViewModel(Army101Data.Create());

        detail.Load(new UnitDetailRequest(Army101Data.PanOceania, joan.Id, "Joan of Arc", "logo.svg", MercsOnly: true, LieutenantOnly: true));

        Assert.Contains(detail.Profiles, p => p.Swc == "+1" && p.SwcDisplay == "SWC +1");
    }

    [Fact]
    public void ApplySelectedProfileTopSummaries_Configuration_MergesItIn()
    {
        var detail = Load();
        var profile = detail.Profiles.First(p => p.UniqueEquipment != "-");

        detail.ApplySelectedProfileTopSummaries(profile);

        Assert.All(UnitDetailText.SummaryValues(profile.UniqueEquipment), item => Assert.Contains(item, detail.EquipmentSummary));
    }

    [Fact]
    public void Reset_Loaded_ReturnsToNoUnit()
    {
        var detail = Load();
        detail.Reset();

        Assert.Equal("Select a unit", detail.UnitNameHeading);
        Assert.Empty(detail.Profiles);
        Assert.Equal("-", detail.Stats.Bs);
        Assert.Null(detail.LogoSource);
    }

    [Fact]
    public void ApplySavedUnitSnapshot_SavedTrooper_OverlaysItsStats()
    {
        var detail = new UnitDetailViewModel(Army101Data.Create());

        detail.ApplySavedUnitSnapshot("Hero", new UnitStatLine { Mov = "4-4", Cc = "15", Bs = "12", Ph = "", Wip = "13", Arm = "1", Bts = "0", Vitality = "1", S = "2" });
        detail.ApplyAugmentStatOverride(new StatOverride(Stat.Ph, 14));

        Assert.Equal(("Hero", "10-10", "15", "12", "14"), (detail.UnitNameHeading, detail.Stats.Mov, detail.Stats.Cc, detail.Stats.Bs, detail.Stats.Ph));
        Assert.Equal(UnitHeaderIcons.None, detail.Icons);
    }

    [Fact]
    public void ShowUnitsInCentimeters_Set_ReformatsMove()
    {
        var detail = new UnitDetailViewModel(Army101Data.Create());
        detail.ApplySavedUnitSnapshot("Hero", new UnitStatLine { Mov = "4-2" });

        detail.ShowUnitsInCentimeters = true;

        Assert.False(detail.ShowUnitsInInches);
        Assert.Equal("10-5", detail.Stats.Mov);
    }
}
