using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Dialogs;

namespace InfinityMercsApp.Services.Rolls;

/// <summary>
/// Rolls on the Booty or Metachemistry table, in the open: the app rolls the d20, or the player enters one rolled
/// with real dice. A (Reroll) skill may roll once more, and the second result is the one kept.
/// </summary>
public sealed class RecruitRollPrompt(IDialogService dialogs, IDice dice, IArmyDataService armyData)
{
    public const string RollChoice = "Roll";
    public const string RealChoice = "Enter a real roll";
    public const string KeepChoice = "Keep";
    public const string RerollChoice = "Reroll";

    /// <summary>
    /// Rolls <paramref name="owed"/> for <paramref name="rollerName"/> (a trooper, or a copy of armour) and shows what
    /// it gives a trooper of <paramref name="unitTypeCode"/>.
    /// </summary>
    /// <param name="cancel">What putting the roll off is called, e.g. "Later".</param>
    /// <returns>The roll kept; null when the roll is put off or the table isn't in the army data.</returns>
    public async Task<RecruitRoll?> RollAsync(string rollerName, OwedRoll owed, string? unitTypeCode, string cancel)
    {
        var table = new RollTable(armyData.GetRecruitTable(owed.Table));
        if (table.Ranges.Count == 0)
        {
            await dialogs.ShowAlertAsync($"No {owed.Table} Table", $"The {owed.Table} table isn't in the army data yet; update the data and try again.");
            return null;
        }

        var how = await dialogs.ChooseAsync($"{rollerName}: roll on the {owed.Table} table", cancel, [RollChoice, RealChoice]);
        if (how is not (RollChoice or RealChoice) || await RollOnceAsync(how) is not { } roll)
        {
            return null;
        }

        var kept = new RecruitRoll(owed.Table, roll, table.Lookup(roll) ?? string.Empty);
        if (owed.CanReroll &&
            await dialogs.ChooseAsync(
                $"{owed.Table} rolled {roll}: {RollEffects.Side(kept.Result, unitTypeCode)}. Keep it, or reroll and keep the second?",
                KeepChoice,
                [RerollChoice]) == RerollChoice &&
            await RollOnceAsync(how) is { } reroll)
        {
            kept = new RecruitRoll(owed.Table, reroll, table.Lookup(reroll) ?? string.Empty);
        }

        await dialogs.ShowAlertAsync($"{owed.Table} rolled {kept.Roll}", $"{rollerName} gains {RollEffects.Side(kept.Result, unitTypeCode)} for good.");
        return kept;
    }

    private async Task<int?> RollOnceAsync(string how) =>
        how == RealChoice ? await dialogs.AskNumberAsync("Real Roll", "What did the d20 show?", 1, 20) : dice.RollD20();
}
