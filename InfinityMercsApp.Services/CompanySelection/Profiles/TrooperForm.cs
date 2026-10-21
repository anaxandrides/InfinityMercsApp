namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>The one form a Transmutation (Auto) trooper is recruited in, with that form's own skills, equipment and characteristics.</summary>
public sealed record TrooperForm(
    UnitProfile Profile,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Equipment,
    IReadOnlyList<string> Characteristics);
