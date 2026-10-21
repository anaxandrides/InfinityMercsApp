namespace InfinityMercsApp.Services.CompanySelection.Teams;

/// <summary>A unit a listed fireteam allows, and how many; <paramref name="Unit"/> is the listed unit it matches, if any.</summary>
public sealed record TeamSlotListing<TUnit>(string Name, string Min, string Max, string? Slug, TUnit? Unit)
    where TUnit : class;
