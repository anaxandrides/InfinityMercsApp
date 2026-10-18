using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Experience;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Season.Items;

/// <summary>A unit in the deployment strip and on the game tracker.</summary>
public sealed partial class DeploymentUnitItem : ObservableObject, IViewerListItem, IUnitTile
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

    /// <summary>A Proxy Pack's Lone Wolf.</summary>
    public bool IsLoneWolf { get; init; }

    /// <summary>For one of the Lone Wolf's Aspects, the Lone Wolf's entry; null for every other trooper.</summary>
    public int? AspectOf { get; init; }
    public bool IsIrregular { get; init; }

    /// <summary>A Cohesive company's trooper with a place in its core fireteam.</summary>
    public bool IsInCoreFireteam { get; init; }
    public bool IsImpetuous { get; init; }
    public bool IsTagFromTagCompany { get; init; }
    public int SlotCost => IsTagFromTagCompany ? 2 : 1;

    /// <summary>One of a Proxy Pack's loadouts: the Lone Wolf's own or an Aspect's.</summary>
    public bool IsPartOfLoneWolf => IsLoneWolf || AspectOf is not null;

    /// <summary>The lieutenant always deploys; a Lone Wolf deploys in whichever of its loadouts the player picks.</summary>
    public bool IsCheckboxEnabled => !IsLieutenant || IsLoneWolf;
    public string CaptainIconPackagedPath { get; init; } = string.Empty;
    public string ExperienceIconPackagedPath { get; init; } = string.Empty;
    public IReadOnlyList<string> LogoCandidates => UnitLogo.Candidates(CachedLogoPath, PackagedLogoPath);

    /// <summary>The selected tile's rail, matching its outline.</summary>
    public Color SelectionAccentColor { get; init; } = Color.FromArgb("#34D399");

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

    /// <summary>A Transmutation (X) trooper's second profile, taken once it has enough wounds; null for anyone else.</summary>
    public TransmutedForm? TransmutesTo { get; init; }

    /// <summary>Wounds taken this game; change them with <see cref="TakeWound"/> and <see cref="HealWound"/>.</summary>
    [ObservableProperty]
    public partial int WoundsReceived { get; set; }

    [ObservableProperty]
    public partial WoundState WoundState { get; set; } = WoundState.Healthy;

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
    public partial bool IsChecked { get; set; }

    /// <summary>The tile being shown in full; the tile view outlines it.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>The trooper's part in the contract, for the post-game pages.</summary>
    /// <param name="deploymentXp">The XP for being deployed: the round number, or 0 when it was not deployed.</param>
    public ExperienceUnitResult ToResult(int deploymentXp, bool isEliteDeployment, bool wasDeployed) => new()
    {
        EntryIndex = EntryIndex,
        Name = Name,
        CachedLogoPath = CachedLogoPath,
        PackagedLogoPath = PackagedLogoPath,
        DeploymentXp = deploymentXp,
        IsEliteDeployment = isEliteDeployment,
        IsConsciousAtEnd = WoundState.IsConscious(),
        WasDeployed = wasDeployed,
        AspectOf = AspectOf,
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
