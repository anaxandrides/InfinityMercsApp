using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Experience;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.ViewModels.UnitBrowser;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>A unit in the deployment strip and on the game tracker.</summary>
public sealed partial class DeploymentUnitItem : ObservableObject, IViewerListItem
{
    // IViewerListItem
    public string Name { get; init; } = string.Empty;
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }
    public string? Subtitle { get; init; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    // CompanyViewerUnitTileView bindings
    public int EntryIndex { get; init; }
    public string BaseUnitDisplayName { get; init; } = string.Empty;
    public bool IsLieutenant { get; init; }
    public bool IsIrregular { get; init; }
    public bool IsImpetuous { get; init; }
    public bool IsTagFromTagCompany { get; init; }
    public int SlotCost => IsTagFromTagCompany ? 2 : 1;
    public bool IsCheckboxEnabled => !IsLieutenant;
    public string CaptainIconPackagedPath { get; init; } = string.Empty;
    public string ExperienceIconPackagedPath { get; init; } = string.Empty;

    // Stats
    public string UnitMov { get; init; } = "-";
    public string UnitCc { get; init; } = "-";
    public string UnitBs { get; init; } = "-";
    public string UnitPh { get; init; } = "-";
    public string UnitWip { get; init; } = "-";
    public string UnitArm { get; init; } = "-";
    public string UnitBts { get; init; } = "-";
    public string UnitVitality { get; init; } = "-";
    public string UnitS { get; init; } = "-";

    /// <summary>VITA for living troopers, STR for TAGs, remotes and peripherals.</summary>
    public Stat VitalityStat { get; init; } = Stat.Vita;
    public string VitalityHeader => VitalityStat.Key();

    /// <summary>The statline in display order: MOV, CC, BS, PH, WIP, ARM, BTS, VITA or STR, S.</summary>
    public IReadOnlyList<StatValue> Statline =>
    [
        new(Stat.Mov, UnitMov),
        new(Stat.Cc, UnitCc),
        new(Stat.Bs, UnitBs),
        new(Stat.Ph, UnitPh),
        new(Stat.Wip, UnitWip),
        new(Stat.Arm, UnitArm),
        new(Stat.Bts, UnitBts),
        new(VitalityStat, UnitVitality),
        new(Stat.S, UnitS)
    ];

    // Wound tracking (used by GameModePage)
    public int StartingVitality { get; init; } = 1;
    public bool HasNwi { get; init; }
    public bool HasRemotePresence { get; init; }
    public int MaxWounds => HasRemotePresence ? StartingVitality + 2 : StartingVitality + 1;

    /// <summary>Wounds taken this game; change them with <see cref="TakeWound"/> and <see cref="HealWound"/>.</summary>
    [ObservableProperty]
    private int _woundsReceived;

    [ObservableProperty]
    private WoundState _woundState = WoundState.Healthy;

    /// <summary>One more wound, up to <see cref="MaxWounds"/>.</summary>
    public void TakeWound() => WoundsReceived = Math.Min(WoundsReceived + 1, MaxWounds);

    /// <summary>One wound fewer, down to none.</summary>
    public void HealWound() => WoundsReceived = Math.Max(WoundsReceived - 1, 0);

    partial void OnWoundsReceivedChanged(int value) =>
        WoundState = WoundStates.For(value, StartingVitality, HasNwi, HasRemotePresence);

    // XP tracking (accumulated across rounds, never reset)
    public UnitXpData XpData { get; } = new();

    /// <summary>The unit's cost, the base cost of its season gear, and its XP.</summary>
    public int Renown { get; init; }

    // Resolved display text
    public string Equipment { get; init; } = "-";
    public string Skills { get; init; } = "-";
    public string RangedWeapons { get; init; } = "-";
    public string MeleeWeapons { get; init; } = "-";

    /// <summary>Ticked to deploy.</summary>
    [ObservableProperty]
    private bool _isChecked;

    /// <summary>The tile being shown in full; the tile view outlines it.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>The trooper's part in the contract, for the post-game pages.</summary>
    /// <param name="deploymentXp">The XP for being deployed: the round number, or 0 when it was not deployed.</param>
    public ExperienceUnitResult ToResult(int deploymentXp, bool isEliteDeployment) => new()
    {
        EntryIndex = EntryIndex,
        Name = Name,
        CachedLogoPath = CachedLogoPath,
        PackagedLogoPath = PackagedLogoPath,
        DeploymentXp = deploymentXp,
        IsEliteDeployment = isEliteDeployment,
        IsConsciousAtEnd = WoundState.IsConscious(),
        GainedInjury = false,
        XpData = XpData,
        IsCaptain = IsLieutenant,
        IsLieutenant = IsLieutenant,
        UnitPh = UnitPh,
        UnitBs = UnitBs,
        UnitCc = UnitCc,
        UnitWip = UnitWip,
        UnitArm = UnitArm,
        Renown = Renown,
        Skills = Skills,
        Equipment = Equipment
    };
}
