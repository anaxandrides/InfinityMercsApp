using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Season.Downtime;
using InfinityMercsApp.Domain.Season.Experience;

namespace InfinityMercsApp.ViewModels.Season.Downtime;

/// <summary>A merc who could perform a downtime choice, with what they lack or the target they would roll.</summary>
public sealed partial class DowntimeParticipantItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isDimmed;

    private DowntimeParticipantItem(ExperienceUnitResult unit, string displayName, IReadOnlyList<string> missing, int? target)
    {
        Unit = unit;
        DisplayName = displayName;
        Missing = missing;
        Target = target;
        _isDimmed = !IsEligible;
    }

    public ExperienceUnitResult Unit { get; }

    public string DisplayName { get; }

    /// <summary>The choice's requirements this merc lacks.</summary>
    public IReadOnlyList<string> Missing { get; }

    public int? Target { get; }

    public bool IsEligible => Missing.Count == 0;

    public string MissingText => IsEligible ? string.Empty : $"Missing: {string.Join(", ", Missing)}";

    public bool ShowTarget => IsEligible && Target.HasValue;

    public string TargetText => Target is { } target ? $"Target {target}" : string.Empty;

    /// <summary>The row for <paramref name="unit"/>; the most renowned merc's row also shows their renown.</summary>
    public static DowntimeParticipantItem For(ExperienceUnitResult unit, DowntimeChoice choice, ParticipantKind kind)
    {
        var name = kind == ParticipantKind.Renowned ? $"{unit.Name}  ({unit.Renown} renown)" : unit.Name;
        var target = choice.Test is { } test ? DowntimeRules.TestTarget(test, unit) : null;
        return new DowntimeParticipantItem(unit, name, DowntimeRules.MissingRequirements(choice, unit), target);
    }
}
