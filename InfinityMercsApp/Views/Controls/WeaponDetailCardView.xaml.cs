using System.Windows.Input;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Presentation.Weapons;
namespace InfinityMercsApp.Views.Controls;

/// <summary>A weapon's stats, special rules (each opening its wiki page) and range bands.</summary>
public partial class WeaponDetailCardView : ContentView
{
    public static readonly BindableProperty WeaponProperty = CardInput(nameof(Weapon), typeof(Weapon), null);
    public static readonly BindableProperty DamageReductionProperty = CardInput(nameof(DamageReduction), typeof(int), 0);
    public static readonly BindableProperty BurstBonusProperty = CardInput(nameof(BurstBonus), typeof(int), 0);
    public static readonly BindableProperty AmmoTypeProperty = CardInput(nameof(AmmoType), typeof(string), null);

    public static readonly BindableProperty ShowUnitsInInchesProperty =
        BindableProperty.Create(nameof(ShowUnitsInInches), typeof(bool), typeof(WeaponDetailCardView), true);
    public static readonly BindableProperty RangeBandHeightRequestProperty =
        BindableProperty.Create(nameof(RangeBandHeightRequest), typeof(double), typeof(WeaponDetailCardView), 88.0);
    public static readonly BindableProperty XVisorActiveProperty =
        BindableProperty.Create(nameof(XVisorActive), typeof(bool), typeof(WeaponDetailCardView), false);
    public static readonly BindableProperty CardProperty =
        BindableProperty.Create(nameof(Card), typeof(WeaponCard), typeof(WeaponDetailCardView));

    public WeaponDetailCardView()
    {
        OpenLinkCommand = new Command<string?>(async url => await OpenLinkAsync(url));
        InitializeComponent();
    }

    public Weapon? Weapon
    {
        get => (Weapon?)GetValue(WeaponProperty);
        set => SetValue(WeaponProperty, value);
    }

    public bool ShowUnitsInInches
    {
        get => (bool)GetValue(ShowUnitsInInchesProperty);
        set => SetValue(ShowUnitsInInchesProperty, value);
    }

    public double RangeBandHeightRequest
    {
        get => (double)GetValue(RangeBandHeightRequestProperty);
        set => SetValue(RangeBandHeightRequestProperty, value);
    }

    public bool XVisorActive
    {
        get => (bool)GetValue(XVisorActiveProperty);
        set => SetValue(XVisorActiveProperty, value);
    }

    public int DamageReduction
    {
        get => (int)GetValue(DamageReductionProperty);
        set => SetValue(DamageReductionProperty, value);
    }

    public int BurstBonus
    {
        get => (int)GetValue(BurstBonusProperty);
        set => SetValue(BurstBonusProperty, value);
    }

    public string? AmmoType
    {
        get => (string?)GetValue(AmmoTypeProperty);
        set => SetValue(AmmoTypeProperty, value);
    }

    /// <summary>What the card shows, rebuilt when the weapon or its modifiers change; null without a weapon.</summary>
    public WeaponCard? Card
    {
        get => (WeaponCard?)GetValue(CardProperty);
        private set => SetValue(CardProperty, value);
    }

    /// <summary>Opens a special rule's wiki page.</summary>
    public ICommand OpenLinkCommand { get; }

    // An input to the card: changing it rebuilds Card.
    private static BindableProperty CardInput(string name, Type type, object? defaultValue) =>
        BindableProperty.Create(name, type, typeof(WeaponDetailCardView), defaultValue,
            propertyChanged: (bindable, _, _) => ((WeaponDetailCardView)bindable).RebuildCard());

    private void RebuildCard() =>
        Card = Weapon is { } weapon ? WeaponCard.From(weapon, DamageReduction, BurstBonus, AmmoType) : null;

    private static async Task OpenLinkAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open '{url}': {ex.Message}");
        }
    }
}
