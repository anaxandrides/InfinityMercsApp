namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>A unit a selection screen lists, before it gets its logos.</summary>
/// <param name="Subtitle">"Type - Category", or "Spec Ops" for a Spec-Ops unit the faction's units don't list.</param>
/// <param name="LogoField">The unit's logo field in the army data, for companies whose units show another unit's logo.</param>
public sealed record SelectionUnitSource(
    int FactionId,
    int UnitId,
    string Name,
    string? Slug,
    int? Type,
    bool IsCharacter,
    string Subtitle,
    bool IsSpecOps,
    string? LogoField);
