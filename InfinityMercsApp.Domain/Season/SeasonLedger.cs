namespace InfinityMercsApp.Domain.Season;

/// <summary>
/// Credits (CR) earned and spent over a season so far, and the company's SWC. SWC is never spent: it caps
/// the SWC of the gear the troopers can have equipped.
/// </summary>
public readonly record struct SeasonLedger(int StartingCr, int CrEarned, int CrSpent, double StartingSwc, double SwcEarned, double SwcBought)
{
    public int CreditsBalance => StartingCr + CrEarned - CrSpent;

    /// <summary>The most SWC of gear the company's troopers can have equipped.</summary>
    public double CompanySwc => StartingSwc + SwcEarned + SwcBought;
}
