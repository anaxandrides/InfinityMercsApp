using InfinityMercsApp.Domain.Models.Army.Fireteams;

namespace InfinityMercsApp.Domain.Company;

/// <summary>
/// A Team-Ops Company: the Team-Ops troopers of one sectorial or vanilla army, two or three of its MI, HI and SpecBot.
/// Each picks one upgrade from the army's Team-Ops table as it joins. The MI or HI chosen at season start is the
/// captain, with the Lieutenant skill, for good. Together they form Fireteam: Team-Ops, a Duo of two or a Haris of
/// three, with no other bonuses.
/// </summary>
public static class TeamOpsCompany
{
    /// <summary>The company type a Team-Ops Company is saved with.</summary>
    public const string CompanyType = "Team-Ops Company";

    public const int MinTroopers = 2;

    public const int MaxTroopers = 3;

    public const string FireteamName = "Fireteam: Team-Ops";

    /// <summary>The fireteam the company's troopers form: a Duo of two, a Haris of three; null for any other number.</summary>
    public static FireteamType? FireteamOf(int troopers) => troopers switch
    {
        2 => FireteamType.Duo,
        3 => FireteamType.Haris,
        _ => null
    };

    /// <summary>Whether a saved company type is a Team-Ops Company.</summary>
    public static bool Is(string? companyType) =>
        string.Equals(companyType?.Trim(), CompanyType, StringComparison.OrdinalIgnoreCase);
}
