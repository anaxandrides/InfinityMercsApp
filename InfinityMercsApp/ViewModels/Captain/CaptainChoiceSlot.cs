using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.ViewModels.Captain;

/// <summary>One weapon, skill or equipment slot; its options are labels such as "(2) - Sniper Rifle".</summary>
public sealed partial class CaptainChoiceSlot : ObservableObject
{
    private readonly Action _selectionChanged;

    [ObservableProperty]
    private string? _selectedChoice = CaptainChoiceLabel.None;

    public CaptainChoiceSlot(IReadOnlyList<string> options, Action selectionChanged)
    {
        Options = options;
        _selectionChanged = selectionChanged;
    }

    /// <summary>"(None)" first, then the available choices.</summary>
    public IReadOnlyList<string> Options { get; }

    /// <summary>The chosen item's name; empty when the slot is unused.</summary>
    public string ChosenName => CaptainChoiceLabel.Name(SelectedChoice);

    public int Cost => CaptainChoiceLabel.Cost(SelectedChoice);

    partial void OnSelectedChoiceChanged(string? value) => _selectionChanged();
}
