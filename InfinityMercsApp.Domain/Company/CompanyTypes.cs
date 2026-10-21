namespace InfinityMercsApp.Domain.Company;

/// <summary>The company types a company file can be saved with.</summary>
public static class CompanyTypes
{
    public const string StandardVanilla = "Standard Company - Vanilla";
    public const string StandardSectorial = "Standard Company - Sectorial";
    public const string Cohesive = "Cohesive Company";
    public const string InspiringLeader = "Inspiring Leader";
    public const string Airborne = "Airborne Company";
    public const string Tag = "TAG Company";

    public static IReadOnlyList<string> All { get; } =
        [StandardVanilla, StandardSectorial, Cohesive, InspiringLeader, Airborne, Tag, ProxyPack.CompanyType];
}
