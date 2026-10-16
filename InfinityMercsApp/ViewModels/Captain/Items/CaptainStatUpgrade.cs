using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanySelection.Captain;

namespace InfinityMercsApp.ViewModels.Captain.Items;

/// <summary>One stat a captain can improve, with its upgrade tiers and the tier currently picked.</summary>
public sealed partial class CaptainStatUpgrade : ObservableObject
{
    // Reports a new pick; a no-op until the constructor has made the first one.
    private Action _selectionChanged = static () => { };

    [ObservableProperty]
    public partial StatPickerOption? SelectedOption { get; set; }

    private CaptainStatUpgrade(Stat stat, int baseValue, CaptainStatChart chart, IReadOnlyList<StatPickerOption> options, Action selectionChanged)
    {
        Stat = stat;
        BaseValue = baseValue;
        Chart = chart;
        Options = options;
        SelectedOption = options.Count > 0 ? options[0] : null;
        _selectionChanged = selectionChanged;
    }

    public Stat Stat { get; }

    /// <summary>The stat's value before any upgrade.</summary>
    public int BaseValue { get; }

    /// <summary>The chart the tiers come from.</summary>
    public CaptainStatChart Chart { get; }

    /// <summary>"No upgrade" first, then each tier with its cumulative cost.</summary>
    public IReadOnlyList<StatPickerOption> Options { get; }

    public int Tier => SelectedOption is { } option ? option.Tier : 0;

    public int Bonus => SelectedOption is { } option ? option.Bonus : 0;

    public int Cost => SelectedOption is { } option ? option.Cost : 0;

    /// <summary>The picked tier, for checking the build.</summary>
    public CaptainStatPick Pick => new(Stat, BaseValue, Tier, Chart);

    /// <summary>The upgrades of <paramref name="stat"/> on <paramref name="chart"/> for a captain whose base value is <paramref name="baseValue"/>.</summary>
    public static CaptainStatUpgrade For(Stat stat, int baseValue, CaptainStatChart chart, Action selectionChanged) =>
        new(stat, baseValue, chart, CaptainUpgradeRules.StatOptions(stat, baseValue, chart).Select(StatPickerOption.From).ToList(), selectionChanged);

    partial void OnSelectedOptionChanged(StatPickerOption? value) => _selectionChanged();
}
