using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Teams;

/// <summary>A listed unit for the fireteam tests.</summary>
internal sealed record ListedUnit(int Id, string Name, string? Slug = null, bool IsCharacter = false, bool IsVisible = true) : ISelectionUnit
{
    public int SourceFactionId => 101;

    public int? Type => 1;

    public bool IsSpecOps => false;
}
