namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>A unit as a company selection screen lists it.</summary>
public interface ISelectionUnit
{
    int Id { get; }
    int SourceFactionId { get; }
    string Name { get; }
    string? Slug { get; }
    int? Type { get; }
    bool IsSpecOps { get; }
    bool IsCharacter { get; }

    /// <summary>Whether the unit shows under the current filter, points and company rules.</summary>
    bool IsVisible { get; }
}
