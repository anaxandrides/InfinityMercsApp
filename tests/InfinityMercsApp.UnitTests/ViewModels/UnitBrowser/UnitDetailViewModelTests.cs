using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.UnitBrowser;

namespace InfinityMercsApp.UnitTests.ViewModels.UnitBrowser;

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
    public void LoadsTheStatLineSummariesAndConfigurations()
    {
        var detail = Load();

        Assert.Equal("Swiss Guard", detail.UnitNameHeading);
        Assert.Equal("logo.svg", detail.LogoSource);
        Assert.NotEqual("-", detail.UnitBs);
        Assert.Contains("-", detail.UnitMov);
        Assert.NotEmpty(detail.Profiles);
        Assert.Equal($"{detail.Profiles.Count} configurations loaded.", detail.ProfilesStatus);
        Assert.StartsWith("Equipment: ", detail.EquipmentSummary);
        Assert.All(detail.Profiles, p => Assert.StartsWith("SWC ", p.SwcDisplay));
    }

    [Fact]
    public void LieutenantOnlyListsLieutenantConfigurations()
    {
        var detail = Load(lieutenantOnly: true);
        Assert.All(detail.Profiles, p => Assert.True(p.IsLieutenant));
    }

    [Fact]
    public void MercsOnlyHidesSwcConfigurations()
    {
        var all = Load();
        var mercs = Load(mercsOnly: true);

        Assert.True(mercs.Profiles.Count < all.Profiles.Count);
        Assert.All(mercs.Profiles, p => Assert.False(ProfileValues.CostsSwc(p.Swc)));
        Assert.All(mercs.Profiles, p => Assert.Equal(string.Empty, p.SwcDisplay));
    }

    [Fact]
    public void MercsOnlyKeepsConfigurationsThatGiveSwc()
    {
        var joan = Army101Data.UnitNamed("JOAN OF ARC");
        var detail = new UnitDetailViewModel(Army101Data.Create());

        detail.Load(new UnitDetailRequest(Army101Data.PanOceania, joan.Id, "Joan of Arc", "logo.svg", MercsOnly: true, LieutenantOnly: true));

        Assert.Contains(detail.Profiles, p => p.Swc == "+1" && p.SwcDisplay == "SWC +1");
    }

    [Fact]
    public void ChoosingAConfigurationMergesItIntoTheSummaries()
    {
        var detail = Load();
        var profile = detail.Profiles.First(p => p.UniqueEquipment != "-");

        detail.ApplySelectedProfileTopSummaries(profile);

        Assert.All(UnitDetailText.SummaryValues(profile.UniqueEquipment), item => Assert.Contains(item, detail.EquipmentSummary));
    }

    [Fact]
    public void ResetReturnsToNoUnit()
    {
        var detail = Load();
        detail.Reset();

        Assert.Equal("Select a unit", detail.UnitNameHeading);
        Assert.Empty(detail.Profiles);
        Assert.Equal("-", detail.UnitBs);
        Assert.Null(detail.LogoSource);
    }

    [Fact]
    public void ASavedTrooperOverlaysItsStats()
    {
        var detail = new UnitDetailViewModel(Army101Data.Create());

        detail.ApplySavedUnitSnapshot("Hero", "4-4", "15", "12", "", "13", "1", "0", "VITA", "1", "2", isLieutenant: true);
        detail.ApplyCaptainStatBonuses(new CaptainStatBonuses(1, 2, 0, 0, 0, 0, 0));
        detail.ApplyAugmentStatOverride(new StatOverride(Stat.Ph, 14));

        Assert.Equal(("Hero", "10-10", "16", "14", "14"), (detail.UnitNameHeading, detail.UnitMov, detail.UnitCc, detail.UnitBs, detail.UnitPh));
        Assert.False(detail.ShowRegularOrderIcon);

        detail.ApplyHackableOverrideFromCurrentConfiguration("Hacking Device", null);
        Assert.True(detail.ShowHackableIcon);
    }

    [Fact]
    public void SwitchingToCentimetresReformatsMove()
    {
        var detail = new UnitDetailViewModel(Army101Data.Create());
        detail.ApplySavedUnitSnapshot("Hero", "4-2", "", "", "", "", "", "", "", "", "", false);

        detail.ShowUnitsInCentimeters = true;

        Assert.False(detail.ShowUnitsInInches);
        Assert.Equal("10-5", detail.UnitMov);
    }
}
