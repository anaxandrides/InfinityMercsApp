namespace InfinityMercsApp.Services.CompanySelection.Teams;

/// <summary>A unit a fireteam allows, as a selection screen's fireteam list shows it.</summary>
public interface ITeamSlot
{
    string Name { get; }
    string? Slug { get; }
    bool IsCharacter { get; }

    /// <summary>The listed unit the slot was matched to when the list was built, if any.</summary>
    int? ResolvedUnitId { get; }

    int? ResolvedSourceFactionId { get; }
}
