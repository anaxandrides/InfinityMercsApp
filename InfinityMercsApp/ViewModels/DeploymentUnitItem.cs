using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Season;

namespace InfinityMercsApp.ViewModels;

/// <summary>A unit in the deployment strip and on the game tracker.</summary>
public sealed class DeploymentUnitItem : ObservableObject, IViewerListItem
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

    private int _woundsReceived;
    public int WoundsReceived
    {
        get => _woundsReceived;
        set
        {
            if (SetProperty(ref _woundsReceived, Math.Clamp(value, 0, MaxWounds)))
            {
                OnPropertyChanged(nameof(WoundStateKey));
            }
        }
    }

    public string WoundStateKey
    {
        get
        {
            var w = _woundsReceived;
            var sv = StartingVitality;
            if (w == 0) return "Healthy";
            if (w < sv) return "Wounded";
            if (w == sv) return HasNwi ? "NwiDown" : "KnockedOut";
            if (w == sv + 1 && HasRemotePresence) return "KnockedOutBody2";
            return "Dead";
        }
    }

    // XP tracking (accumulated across rounds, never reset)
    public UnitXpData XpData { get; } = new();

    // Renown — unit cost (loadout-inclusive) + accumulated XP from the saved file.
    public int Renown { get; init; }

    // Resolved display text
    public string Equipment { get; init; } = "-";
    public string Skills { get; init; } = "-";
    public string RangedWeapons { get; init; } = "-";
    public string MeleeWeapons { get; init; } = "-";

    // Deployment checkbox
    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }

    // Tile selection highlight
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(TileStroke));
                OnPropertyChanged(nameof(TileStrokeThickness));
            }
        }
    }

    public Color TileStroke => IsSelected ? Color.FromArgb("#34D399") : Color.FromArgb("#3A4554");
    public double TileStrokeThickness => IsSelected ? 2.0 : 1.0;
}
