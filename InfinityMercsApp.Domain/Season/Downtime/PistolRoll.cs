namespace InfinityMercsApp.Domain.Season.Downtime;

/// <summary>A pistol won in downtime: the d20 rolled on the pistol table and any SD bonus a critical pass adds.</summary>
public sealed record PistolRoll(string Name, int Roll, int SdBonus)
{
    public string Display => $"{Name}{(SdBonus > 0 ? $" (+{SdBonus} SD)" : "")} (rolled {Roll})";
}
