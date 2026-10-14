namespace InfinityMercsApp.Domain.Models.Perks;

public sealed class CompanyPerkRollRange
{
    public int Min { get; init; }
    public int Max { get; init; }

    public bool Contains(int roll)
    {
        return roll >= Min && roll <= Max;
    }

    public override string ToString()
    {
        return Min == Max ? Min.ToString() : $"{Min}-{Max}";
    }
}
