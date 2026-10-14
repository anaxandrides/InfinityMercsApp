using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Season.Experience;
using InfinityMercsApp.Services.Logos;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>One deployed trooper's XP card; exactly one trooper can be the MVP.</summary>
public partial class ExperienceUnitItem(ExperienceUnitResult unit) : ObservableObject
{
    public ExperienceUnitResult Unit { get; } = unit;

    public string Name => Unit.Name;

    public string? LogoSource { get; } = UnitLogo.Source(unit.CachedLogoPath, unit.PackagedLogoPath);

    public IReadOnlyList<XpLine> XpLines { get; } = MissionExperience.Lines(unit);

    public int MvpXp => MissionExperience.MvpXp;

    public string TotalText => $"Total: {Unit.TotalXp} XP";

    public bool IsMvp
    {
        get => Unit.IsMvp;
        set
        {
            if (Unit.IsMvp == value)
            {
                return;
            }

            Unit.IsMvp = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalText));
        }
    }
}
