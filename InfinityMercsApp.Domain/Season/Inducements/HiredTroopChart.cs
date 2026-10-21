namespace InfinityMercsApp.Domain.Season.Inducements;

/// <summary>The Troops for Hire table: each trooper, the loadouts it can be hired in, and its cost in inducements.</summary>
public static class HiredTroopChart
{
    private const string Any = "Any";

    public static IReadOnlyList<HiredTroopOffer> Offers { get; } =
    [
        new("Authorized Bounty Hunter", "AUTHORIZED BOUNTY HUNTER", "SMG or BSG", ["Submachine Gun", "Boarding Shotgun"], 10, []),
        new("Monstruckers", "MONSTRUCKER", Any, [], 15, []),
        new("Motorized Bounty Hunters", "MOTORIZED BOUNTY HUNTER", Any, [], 15, []),
        new("Wardriver", "WARDRIVER", Any, [], 15, []),
        new("Bashi Bazouks", "BASHI BAZOUK", Any, [], 15, []),
        new("Authorized Bounty Hunter", "AUTHORIZED BOUNTY HUNTER", "Sniper or Spitfire", ["Sniper", "Spitfire"], 20, []),
        new("Digger", "DIGGER", Any, [], 20, []),
        new("Brawler", "BRAWLER", "Sniper", ["Sniper"], 25, []),
        new("Freelance Operator Samsa", "FREELANCE OPERATOR SAMSA", Any, [], 30, []),
        new("Anaconda, Mercenary TAG Squad", "ANACONDA", "Any + Gains Immunity (POS)", [], 50, ["Immunity (POS)"])
    ];
}
