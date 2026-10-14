namespace InfinityMercsApp.Services.CompanySelection.Captain;

public sealed class CaptainUpgradeOptionSet
{
    public static CaptainUpgradeOptionSet Empty { get; } = new();
    public List<string> Weapons { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<string> Equipment { get; init; } = [];
    public bool IsEmpty => Weapons.Count == 0 && Skills.Count == 0 && Equipment.Count == 0;
}
