using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Season.Experience;
using InfinityMercsApp.Services.Logos;

namespace InfinityMercsApp.ViewModels.Season.Items;

/// <summary>One deployed trooper's XP card; exactly one trooper can be the MVP.</summary>
public partial class ExperienceUnitItem : ObservableObject
{
    public ExperienceUnitItem(ExperienceUnitResult unit)
    {
        Unit = unit;
        LogoSource = UnitLogo.Source(unit.CachedLogoPath, unit.PackagedLogoPath);
        XpLines = MissionExperience.Lines(unit);
        IsMvp = unit.IsMvp;
        TotalText = TotalTextOf(unit);
    }

    public ExperienceUnitResult Unit { get; }

    public string Name => Unit.Name;

    public string? LogoSource { get; }

    public IReadOnlyList<XpLine> XpLines { get; }

    public int MvpXp => MissionExperience.MvpXp;

    /// <summary>"Total: 9 XP", the MVP's included.</summary>
    [ObservableProperty]
    public partial string TotalText { get; set; }

    [ObservableProperty]
    public partial bool IsMvp { get; set; }

    // The result carries the MVP choice on to the season, and the total takes it in.
    partial void OnIsMvpChanged(bool value)
    {
        Unit.IsMvp = value;
        TotalText = TotalTextOf(Unit);
    }

    private static string TotalTextOf(ExperienceUnitResult unit) => $"Total: {unit.TotalXp} XP";
}
