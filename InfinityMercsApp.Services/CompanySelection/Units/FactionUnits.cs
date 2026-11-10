using InfinityMercsApp.Domain.Models.Army.Fireteams;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>One source faction's units, the Spec-Ops it offers as captain, type and category names, and fireteams.</summary>
public sealed record FactionUnits(
    int FactionId,
    IReadOnlyList<ArmyResumeRecord> Resumes,
    IReadOnlyList<ArmyResumeRecord> SpecOps,
    IReadOnlyDictionary<int, string> TypeLookup,
    IReadOnlyDictionary<int, string> CategoryLookup,
    FireteamChart? FireteamChart);
