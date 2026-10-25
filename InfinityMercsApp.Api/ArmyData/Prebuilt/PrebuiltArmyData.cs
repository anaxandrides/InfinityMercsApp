using InfinityMercsApp.Infrastructure.API.ArmyMirror;

namespace InfinityMercsApp.Api.ArmyData.Prebuilt;

/// <summary>What the API works out ahead from a snapshot's armies, each kept as a payload under its hash.</summary>
/// <param name="Generated">The Airborne, Inspiring and TAG companies' factions.</param>
/// <param name="Listings">Which factions the Airborne, Inspiring and Cohesive companies list (FactionListings).</param>
public sealed record PrebuiltArmyData(IReadOnlyList<MirroredArmy> Generated, MirroredPayload Listings);
