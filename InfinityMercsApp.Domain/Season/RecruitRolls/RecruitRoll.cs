namespace InfinityMercsApp.Domain.Season.RecruitRolls;

/// <summary>
/// A trooper's roll on a recruit table: the d20 it kept and the table's result for it, written as the table writes
/// it ("TAG: … / Other Troop Types: …" rows included). The trooper keeps it for good.
/// </summary>
public sealed record RecruitRoll(RecruitRollTable Table, int Roll, string Result)
{
    /// <summary>What it gives a trooper of <paramref name="unitTypeCode"/>, e.g. "Booty: DA CC Weapon".</summary>
    public string Describe(string? unitTypeCode) => $"{Table}: {RollEffects.Side(Result, unitTypeCode)}";
}
