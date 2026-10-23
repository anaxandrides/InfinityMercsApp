namespace InfinityMercsApp.Api.ArmyData.Logos;

/// <summary>One logo for the bundle: where it goes in the app's logo cache, and where Corvus Belli serves it.</summary>
/// <param name="Path">The path in the bundle, as the app's cache names it: "{factionId}.svg" or "units/{factionId}-{unitId}.svg".</param>
public sealed record LogoFile(string Path, string Url);
