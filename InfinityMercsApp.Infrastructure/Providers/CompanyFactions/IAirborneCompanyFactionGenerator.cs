namespace InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

/// <summary>
/// Generates the synthetic "Airborne Company" faction (ID 2001) by scanning all stored factions
/// for units with the Airborne Deployment skill, then writing the result to the database.
/// </summary>
public interface IAirborneCompanyFactionGenerator : ICompanyFactionGenerator
{
}
