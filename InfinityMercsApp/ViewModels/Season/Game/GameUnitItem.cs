using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.Domain.Season.Injuries;

namespace InfinityMercsApp.ViewModels.Season.Game;

/// <summary>A deployed trooper's row on the game tracker: wounds, XP, its Tactical Awareness token and its weapons.</summary>
public sealed partial class GameUnitItem : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArrowText))]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isTacticalTokenSpent;

    public GameUnitItem(DeploymentUnitItem unit, bool hasTacticalToken, IReadOnlyList<GameWeaponCard> weaponCards)
    {
        Unit = unit;
        HasTacticalToken = hasTacticalToken;
        WeaponCards = weaponCards;
        Skills = AttackModifiers.NormalizeSkills(unit.Skills);
        unit.PropertyChanged += OnUnitPropertyChanged;
    }

    public DeploymentUnitItem Unit { get; }

    public string Name => Unit.Name;

    public string ArrowText => IsExpanded ? "▼" : "▶";

    public IReadOnlyList<StatValue> Statline => Unit.Statline;

    /// <summary>The skills with BS Attack written the canonical way.</summary>
    public string Skills { get; }

    public IReadOnlyList<string> EquipmentItems => SortedItems(Unit.Equipment);

    public IReadOnlyList<string> SkillItems => SortedItems(Skills);

    public IReadOnlyList<GameWeaponCard> WeaponCards { get; }

    /// <summary>An Elite Deployment gives troopers (and a lieutenant with Tactical Awareness) a token to spend.</summary>
    public bool HasTacticalToken { get; }

    public WoundState WoundState => Unit.WoundState;

    public bool IsConscious => WoundState.IsConscious();

    public string WoundIconPath => WoundState switch
    {
        WoundState.Healthy => "SVGCache/NonCBIcons/noun-smiley-face.svg",
        WoundState.Wounded => "SVGCache/NonCBIcons/noun-sad-face.svg",
        WoundState.NwiDown => "SVGCache/NonCBIcons/noun-mad-face.svg",
        WoundState.KnockedOut or WoundState.KnockedOutBody2 => "SVGCache/NonCBIcons/noun-knocked-out-face.svg",
        _ => "SVGCache/NonCBIcons/noun-gravestone.svg"
    };

    /// <summary>A Remote Presence trooper knocked out on its second body shows a "2" on its icon.</summary>
    public bool IsOnSecondBody => WoundState == WoundState.KnockedOutBody2;

    public string XpButtonText => Unit.XpData.TotalXp > 0 ? $"XP ({Unit.XpData.TotalXp})" : "XP";

    public OrderSource OrderSource => new(Unit.IsLieutenant, Unit.IsImpetuous, Unit.IsIrregular, IsConscious);

    [RelayCommand]
    private void TakeWound() => Unit.WoundsReceived++;

    [RelayCommand]
    private void HealWound() => Unit.WoundsReceived--;

    [RelayCommand]
    private void ToggleTacticalToken() => IsTacticalTokenSpent = !IsTacticalTokenSpent;

    /// <summary>A new round: a token comes back unless its trooper is out of action.</summary>
    public void ResetTacticalToken() => IsTacticalTokenSpent = !IsConscious;

    public void RefreshXp() => OnPropertyChanged(nameof(XpButtonText));

    private void OnUnitPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeploymentUnitItem.WoundState))
        {
            OnPropertyChanged(nameof(WoundState));
            OnPropertyChanged(nameof(WoundIconPath));
            OnPropertyChanged(nameof(IsOnSecondBody));
            OnPropertyChanged(nameof(IsConscious));
        }
    }

    private static IReadOnlyList<string> SortedItems(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0 && item != "-")
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
}
