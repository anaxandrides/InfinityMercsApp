namespace InfinityMercsApp.Presentation.Company.Selection;

/// <summary>
/// A faction generated for a kind of company (Airborne, Inspiring): it has no downloaded logo, only the packaged
/// one the company kind names.
/// </summary>
public sealed record GeneratedFactionLogo(int FactionId, string LogoPath);
