using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Season.Experience;

namespace InfinityMercsApp.ViewModels.Season.Game;

/// <summary>
/// The XP a trooper earned this game, as checkboxes over its <see cref="UnitXpData"/>. Numbered rows are ticked
/// in order; succeeding needs an attempt, and scanning with a forward observer needs a scan.
/// </summary>
public sealed partial class XpSheet : ObservableObject
{
    private readonly UnitXpData _xp;
    private readonly Action<int>? _totalChanged;

    /// <param name="totalChanged">Told the new total whenever a box changes it.</param>
    public XpSheet(string unitName, UnitXpData xp, Action<int>? totalChanged = null)
    {
        _xp = xp;
        _totalChanged = totalChanged;
        _totalXp = xp.TotalXp;
        _totalText = TotalTextOf(xp.TotalXp);
        UnitName = unitName;
        Assist = Numbered(xp.Assist);
        InflictState = Numbered(xp.InflictState);
        Attempted = new XpCheck("Attempted", xp.AttemptButton, OnAttemptedChanged);
        Succeeded = new XpCheck("Succeeded", xp.SucceedButton, OnSucceededChanged);
        Scanned = new XpCheck("Scanned", xp.ScanEnemy, OnScannedChanged);
        ScannedWithFo = new XpCheck("Scanned with FO", xp.ScanEnemyFo, OnScannedWithFoChanged);
        TagAndBag = new XpCheck("Tagged and Bagged", xp.TagAndBag, check => Update(() => _xp.TagAndBag = check.IsChecked));
    }

    public string UnitName { get; }

    public IReadOnlyList<XpCheck> Assist { get; }

    public IReadOnlyList<XpCheck> InflictState { get; }

    public XpCheck Attempted { get; }

    public XpCheck Succeeded { get; }

    public XpCheck Scanned { get; }

    public XpCheck ScannedWithFo { get; }

    public XpCheck TagAndBag { get; }

    [ObservableProperty]
    private int _totalXp;

    [ObservableProperty]
    private string _totalText;

    // Ticks fill from the left and clear from the right: only the last ticked and the first unticked box can change.
    private IReadOnlyList<XpCheck> Numbered(IList<bool> values)
    {
        var checks = new List<XpCheck>();
        for (var i = 0; i < values.Count; i++)
        {
            var index = i;
            checks.Add(new XpCheck($"#{i + 1}", values[i], check => Update(() =>
            {
                values[index] = check.IsChecked;
                EnableEnds(checks);
            })));
        }

        EnableEnds(checks);
        return checks;
    }

    private static void EnableEnds(IReadOnlyList<XpCheck> checks)
    {
        var lastChecked = checks.Select((check, i) => check.IsChecked ? i : -1).Max();
        var firstUnchecked = checks.Select((check, i) => check.IsChecked ? int.MaxValue : i).DefaultIfEmpty(-1).Min();
        for (var i = 0; i < checks.Count; i++)
        {
            checks[i].IsEnabled = i == lastChecked || i == firstUnchecked;
        }
    }

    private void OnAttemptedChanged(XpCheck check) => Update(() =>
    {
        _xp.AttemptButton = check.IsChecked;
        if (!check.IsChecked)
        {
            Succeeded.IsChecked = false;
        }
    });

    private void OnSucceededChanged(XpCheck check) => Update(() =>
    {
        _xp.SucceedButton = check.IsChecked;
        if (check.IsChecked)
        {
            Attempted.IsChecked = true;
        }
    });

    private void OnScannedChanged(XpCheck check) => Update(() =>
    {
        _xp.ScanEnemy = check.IsChecked;
        if (!check.IsChecked)
        {
            ScannedWithFo.IsChecked = false;
        }
    });

    private void OnScannedWithFoChanged(XpCheck check) => Update(() =>
    {
        _xp.ScanEnemyFo = check.IsChecked;
        if (check.IsChecked)
        {
            Scanned.IsChecked = true;
        }
    });

    // Applies a box change, then works out the total again.
    private void Update(Action change)
    {
        change();
        TotalXp = _xp.TotalXp;
        TotalText = TotalTextOf(TotalXp);
        _totalChanged?.Invoke(TotalXp);
    }

    private static string TotalTextOf(int totalXp) => $"Total XP Earned: {totalXp}";
}
