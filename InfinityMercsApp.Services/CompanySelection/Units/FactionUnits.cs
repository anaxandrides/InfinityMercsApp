using InfinityMercsApp.Domain.Models.Army.Fireteams;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecOps.SpecopsUnit;

namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>One source faction's units, Spec-Ops units, type and category names, and fireteams.</summary>
public sealed record FactionUnits(
    int FactionId,
    IReadOnlyList<ArmyResumeRecord> Resumes,
    IReadOnlyList<ArmySpecopsUnitRecord> SpecOps,
    IReadOnlyDictionary<int, string> TypeLookup,
    IReadOnlyDictionary<int, string> CategoryLookup,
    FireteamChart? FireteamChart);
