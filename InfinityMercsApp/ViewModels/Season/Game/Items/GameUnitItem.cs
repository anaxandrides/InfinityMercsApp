using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.ViewModels.Season.Items;

namespace InfinityMercsApp.ViewModels.Season.Game.Items;

/// <summary>A deployed trooper's row on the game tracker: wounds, XP, its Tactical Awareness token and its weapons.</summary>
public sealed partial class GameUnitItem : ObservableObject
{
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsTacticalTokenSpent { get; set; }

    public GameUnitItem(DeploymentUnitItem unit, bool hasTacticalToken, IReadOnlyList<GameWeaponCard> weaponCards)
    {
        Unit = unit;
        HasTacticalToken = hasTacticalToken;
        WeaponCards = weaponCards;
        Skills = AttackModifiers.NormalizeSkills(unit.Skills);
        XpButtonText = XpText(unit.XpData.TotalXp);
        ShowWoundState(unit.WoundState);
        unit.PropertyChanged += OnUnitPropertyChanged;
    }

    public DeploymentUnitItem Unit { get; }

    public string Name => Unit.Name;

    public IReadOnlyList<StatValue> Statline => Unit.Statline;

    /// <summary>The skills with BS Attack written the canonical way.</summary>
    public string Skills { get; }

    public IReadOnlyList<string> EquipmentItems => SortedItems(Unit.Equipment);

    public IReadOnlyList<string> SkillItems => SortedItems(Skills);

    public IReadOnlyList<GameWeaponCard> WeaponCards { get; }

    /// <summary>An Elite Deployment gives troopers (and a lieutenant with Tactical Awareness) a token to spend.</summary>
    public bool HasTacticalToken { get; }

    [ObservableProperty]
    public partial WoundState WoundState { get; set; }

    [ObservableProperty]
    public partial bool IsConscious { get; set; }

    [ObservableProperty]
    public partial string WoundIconPath { get; set; } = string.Empty;

    /// <summary>A Remote Presence trooper knocked out on its second body shows a "2" on its icon.</summary>
    [ObservableProperty]
    public partial bool IsOnSecondBody { get; set; }

    /// <summary>"XP", or "XP (3)" once the trooper has earned some.</summary>
    [ObservableProperty]
    public partial string XpButtonText { get; set; }

    public OrderSource OrderSource => new(Unit.IsLieutenant, Unit.IsImpetuous, Unit.IsIrregular, IsConscious);

    [RelayCommand]
    private void TakeWound() => Unit.TakeWound();

    [RelayCommand]
    private void HealWound() => Unit.HealWound();

    [RelayCommand]
    private void ToggleTacticalToken() => IsTacticalTokenSpent = !IsTacticalTokenSpent;

    /// <summary>A new round: a token comes back unless its trooper is out of action.</summary>
    public void ResetTacticalToken() => IsTacticalTokenSpent = !IsConscious;

    /// <summary>The trooper's XP sheet; ticking its boxes updates the XP button as they go.</summary>
    public XpSheet XpSheet() => new(Name, Unit.XpData, total => XpButtonText = XpText(total));

    private void OnUnitPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeploymentUnitItem.WoundState))
        {
            ShowWoundState(Unit.WoundState);
        }
    }

    private void ShowWoundState(WoundState state)
    {
        WoundState = state;
        IsConscious = state.IsConscious();
        IsOnSecondBody = state == WoundState.KnockedOutBody2;
        WoundIconPath = state switch
        {
            WoundState.Healthy => "SVGCache/NonCBIcons/noun-smiley-face.svg",
            WoundState.Wounded => "SVGCache/NonCBIcons/noun-sad-face.svg",
            WoundState.NwiDown => "SVGCache/NonCBIcons/noun-mad-face.svg",
            WoundState.KnockedOut or WoundState.KnockedOutBody2 => "SVGCache/NonCBIcons/noun-knocked-out-face.svg",
            _ => "SVGCache/NonCBIcons/noun-gravestone.svg"
        };
    }

    private static string XpText(int totalXp) => totalXp > 0 ? $"XP ({totalXp})" : "XP";

    private static IReadOnlyList<string> SortedItems(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0 && item != "-")
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
}
