namespace InfinityMercsApp.Domain.Season;

/// <summary>Credits (CR) and support-weapon cost (SWC) earned, bought and spent over a season so far.</summary>
public readonly record struct SeasonLedger(int CrEarned, int CrSpent, double SwcEarned, double SwcBought, double SwcSpent)
{
    public int CreditsBalance => CrEarned - CrSpent;

    public double SwcBalance => SwcEarned + SwcBought - SwcSpent;
}
