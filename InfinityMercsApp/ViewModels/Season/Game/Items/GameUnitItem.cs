using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Presentation.Season.Game;
using InfinityMercsApp.ViewModels.Season.Items;

namespace InfinityMercsApp.ViewModels.Season.Game.Items;

/// <summary>
/// A deployed trooper's row on the game tracker: wounds, XP, its Tactical Awareness token and its weapons. A Lone
/// Wolf's loadouts each have a row; the one the Lone Wolf is in shows its profile, and the rest show the Aspect of the
/// Wolf's until the Lone Wolf transfers into them. A Transmutation (X) trooper shows its second profile while it has
/// taken enough wounds, keeping its weapons and the rest of its gear.
/// </summary>
public sealed partial class GameUnitItem : ObservableObject
{
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsTacticalTokenSpent { get; set; }

    private readonly IReadOnlyList<GameWeaponCard> _weaponCards;

    public GameUnitItem(DeploymentUnitItem unit, bool hasTacticalToken, IReadOnlyList<GameWeaponCard> weaponCards)
    {
        Unit = unit;
        HasTacticalToken = hasTacticalToken;
        _weaponCards = weaponCards;
        HostsLoneWolf = unit.IsLoneWolf;
        Skills = AttackModifiers.NormalizeSkills(unit.Skills);
        XpButtonText = XpText(unit.XpData.TotalXp);
        ShowWoundState(unit.WoundState);
        unit.PropertyChanged += OnUnitPropertyChanged;
    }

    public DeploymentUnitItem Unit { get; }

    public string Name => Unit.Name;

    /// <summary>One of a Proxy Pack's loadouts: the Lone Wolf's own or an Aspect's.</summary>
    public bool IsPartOfLoneWolf => Unit.IsLoneWolf || Unit.AspectOf is not null;

    /// <summary>The Lone Wolf is in this loadout's position.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(ShowsAspect), nameof(LoneWolfRole), nameof(CanReceiveLoneWolf), nameof(Statline),
        nameof(EquipmentItems), nameof(SkillItems), nameof(WeaponCards), nameof(OrderSource))]
    public partial bool HostsLoneWolf { get; set; }

    /// <summary>An Aspect the Lone Wolf isn't in, showing the Aspect of the Wolf profile.</summary>
    public bool ShowsAspect => IsPartOfLoneWolf && !HostsLoneWolf;

    /// <summary>"LONE WOLF" or "ASPECT" on a Proxy Pack's rows; nothing on anyone else's.</summary>
    public string LoneWolfRole => !IsPartOfLoneWolf ? string.Empty : HostsLoneWolf ? "LONE WOLF" : "ASPECT";

    /// <summary>The Lone Wolf can transfer here: an Aspect still standing.</summary>
    public bool CanReceiveLoneWolf => ShowsAspect && IsConscious;

    /// <summary>A Transmutation (X) trooper with enough wounds to be in its second profile.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Statline), nameof(EquipmentItems), nameof(SkillItems), nameof(FormName))]
    public partial bool IsTransmuted { get; set; }

    /// <summary>The second profile's name ("BATTLE-RAVAGED") while it is in it; empty otherwise.</summary>
    public string FormName => IsTransmuted && Unit.TransmutesTo is { } form ? form.Name.ToUpperInvariant() : string.Empty;

    public IReadOnlyList<StatValue> Statline =>
        ShowsAspect ? AspectOfTheWolf.Statline(Unit.UnitVitality)
        : IsTransmuted && Unit.TransmutesTo is { } form ? form.Statline
        : Unit.Statline;

    /// <summary>The skills with BS Attack written the canonical way.</summary>
    public string Skills { get; }

    public IReadOnlyList<string> EquipmentItems =>
        ShowsAspect ? []
        : IsTransmuted && Unit.TransmutesTo is { } form ? Sorted(form.Equipment)
        : SortedItems(Unit.Equipment);

    public IReadOnlyList<string> SkillItems =>
        ShowsAspect ? AspectOfTheWolf.Skills
        : IsTransmuted && Unit.TransmutesTo is { } form ? Sorted(form.Skills)
        : SortedItems(Skills);

    /// <summary>The loadout's weapons; an Aspect the Lone Wolf isn't in can't use them.</summary>
    public IReadOnlyList<GameWeaponCard> WeaponCards => ShowsAspect ? [] : _weaponCards;

    /// <summary>An Elite Deployment gives troopers (and a lieutenant with Tactical Awareness) a token to spend.</summary>
    public bool HasTacticalToken { get; }

    /// <summary>
    /// A Cohesive company may never spend Tactical Awareness on its core fireteam, so a token held by one of its
    /// troopers carries a reminder: the app doesn't know which troopers formed the fireteam in play.
    /// </summary>
    public bool ShowsCoreFireteamTacticalNote => HasTacticalToken && Unit.IsInCoreFireteam;

    [ObservableProperty]
    public partial WoundState WoundState { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanReceiveLoneWolf))]
    public partial bool IsConscious { get; set; }

    [ObservableProperty]
    public partial string WoundIconPath { get; set; } = string.Empty;

    /// <summary>A Remote Presence trooper knocked out on its second body shows a "2" on its icon.</summary>
    [ObservableProperty]
    public partial bool IsOnSecondBody { get; set; }

    /// <summary>"XP", or "XP (3)" once the trooper has earned some.</summary>
    [ObservableProperty]
    public partial string XpButtonText { get; set; }

    /// <summary>The orders it gives; the Lone Wolf's Lieutenant order goes where the Lone Wolf is.</summary>
    public OrderSource OrderSource => new(
        IsPartOfLoneWolf ? HostsLoneWolf : Unit.IsLieutenant,
        !ShowsAspect && Unit.IsImpetuous,
        !ShowsAspect && Unit.IsIrregular,
        IsConscious);

    /// <summary>Whether this row takes the injury check when it dies: anyone but an Aspect the Lone Wolf isn't in.</summary>
    public bool ChecksForInjury => !ShowsAspect;

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
        if (e.PropertyName is nameof(DeploymentUnitItem.WoundState) or nameof(DeploymentUnitItem.WoundsReceived))
        {
            ShowWoundState(Unit.WoundState);
        }
    }

    private void ShowWoundState(WoundState state)
    {
        WoundState = state;
        IsConscious = state.IsConscious();
        IsOnSecondBody = state == WoundState.KnockedOutBody2;
        IsTransmuted = Unit.TransmutesTo is { } form && Unit.WoundsReceived >= form.Threshold;
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
        string.IsNullOrWhiteSpace(text) ? [] : Sorted(text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    private static IReadOnlyList<string> Sorted(IEnumerable<string> items) =>
        items.Select(item => item.Trim())
            .Where(item => item.Length > 0 && item != "-")
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
