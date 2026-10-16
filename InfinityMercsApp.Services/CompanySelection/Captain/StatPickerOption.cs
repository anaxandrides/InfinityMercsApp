using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Services.CompanySelection.Captain;

/// <summary>A captain stat upgrade level as a picker item.</summary>
public sealed record StatPickerOption(Stat Stat, int Tier, int Bonus, int Cost)
{
    public string Label => $"{Stat.Key()} +{Bonus} | {Cost}xp";

    public static StatPickerOption From(InfinityMercsApp.Domain.Captain.CaptainStatOption option) =>
        new(option.Stat, option.Tier, option.Bonus, option.Cost);
}
