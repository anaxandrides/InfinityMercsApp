using InfinityMercsApp.Services.CompanySelection.StartCompany;

namespace InfinityMercsApp.Services.CompanySelection.Captain;

/// <summary>
/// Which faction a captain or TAG popup takes its upgrade options from: the trooper's own faction, else the
/// company's first source faction. <paramref name="Known"/> are the factions the screen already has, for their
/// names and parents; the army metadata covers the rest.
/// </summary>
/// <param name="PreferredOtherFactionId">For a TAG: the sectorial chosen beside the TAG company, when there is one.</param>
public sealed record CaptainFactions(
    int TrooperFactionId,
    int? FirstSourceFactionId,
    IReadOnlyList<ICompanySourceFaction> Known,
    int? PreferredOtherFactionId = null);
