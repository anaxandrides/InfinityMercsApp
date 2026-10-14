using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanySelection.Captain;

namespace InfinityMercsApp.ViewModels.Captain;

/// <summary>One stat a captain can improve, with its upgrade tiers and the tier currently picked.</summary>
public sealed partial class CaptainStatUpgrade : ObservableObject
{
    private readonly Action _selectionChanged;

    [ObservableProperty]
    private StatPickerOption? _selectedOption;

    private CaptainStatUpgrade(Stat stat, IReadOnlyList<StatPickerOption> options, Action selectionChanged)
    {
        Stat = stat;
        Options = options;
        _selectionChanged = selectionChanged;
        _selectedOption = options.Count > 0 ? options[0] : null;
    }

    public Stat Stat { get; }

    /// <summary>"No upgrade" first, then each tier with its cumulative cost.</summary>
    public IReadOnlyList<StatPickerOption> Options { get; }

    public int Tier => SelectedOption is { } option ? option.Tier : 0;

    public int Bonus => SelectedOption is { } option ? option.Bonus : 0;

    public int Cost => SelectedOption is { } option ? option.Cost : 0;

    /// <summary>The upgrades of <paramref name="stat"/> for a captain whose base value is <paramref name="baseValue"/>.</summary>
    public static CaptainStatUpgrade For(Stat stat, int baseValue, Action selectionChanged) =>
        new(stat, CaptainUpgradeRules.StatOptions(stat, baseValue).Select(StatPickerOption.From).ToList(), selectionChanged);

    partial void OnSelectedOptionChanged(StatPickerOption? value) => _selectionChanged();
}
