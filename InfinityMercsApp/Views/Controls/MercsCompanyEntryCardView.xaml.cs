using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.Views.Controls;

/// <summary>A roster entry on the selection screen: tap to select, X to remove, the chevron to show its details.</summary>
public partial class MercsCompanyEntryCardView : ContentView
{
    public static readonly BindableProperty SelectEntryCommandProperty =
        BindableProperty.Create(
            nameof(SelectEntryCommand),
            typeof(ICommand),
            typeof(MercsCompanyEntryCardView));

    public static readonly BindableProperty RemoveEntryCommandProperty =
        BindableProperty.Create(
            nameof(RemoveEntryCommand),
            typeof(ICommand),
            typeof(MercsCompanyEntryCardView));

    public static readonly BindableProperty ShowOrderModifierBadgesProperty =
        BindableProperty.Create(
            nameof(ShowOrderModifierBadges),
            typeof(bool),
            typeof(MercsCompanyEntryCardView),
            false);

    public MercsCompanyEntryCardView()
    {
        InitializeComponent();
    }

    public ICommand? SelectEntryCommand
    {
        get => (ICommand?)GetValue(SelectEntryCommandProperty);
        set => SetValue(SelectEntryCommandProperty, value);
    }

    public ICommand? RemoveEntryCommand
    {
        get => (ICommand?)GetValue(RemoveEntryCommandProperty);
        set => SetValue(RemoveEntryCommandProperty, value);
    }

    public bool ShowOrderModifierBadges
    {
        get => (bool)GetValue(ShowOrderModifierBadgesProperty);
        set => SetValue(ShowOrderModifierBadgesProperty, value);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        SyncChevronRotation(animated: false);
    }

    /// <summary>Shows or hides the trooper's details, turning the chevron to match.</summary>
    [RelayCommand]
    private async Task ToggleDetailsAsync()
    {
        if (BindingContext is not MercsCompanyEntry entry)
        {
            return;
        }

        entry.IsDetailsExpanded = !entry.IsDetailsExpanded;
        await SyncChevronRotationAsync(animated: true);
    }

    private void SyncChevronRotation(bool animated)
    {
        _ = SyncChevronRotationAsync(animated);
    }

    private async Task SyncChevronRotationAsync(bool animated)
    {
        if (BindingContext is not MercsCompanyEntry entry)
        {
            return;
        }

        var targetRotation = entry.IsDetailsExpanded ? 90d : 0d;
        DetailsToggleButton.CancelAnimations();

        if (animated)
        {
            await DetailsToggleButton.RotateToAsync(targetRotation, 140, Easing.CubicOut);
            return;
        }

        DetailsToggleButton.Rotation = targetRotation;
    }
}
