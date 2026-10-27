namespace InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

/// <summary>
/// Generates the synthetic "Inspiring Company" faction (ID 2002) by scanning all stored factions
/// for units with the Inspiring Leadership skill, then writing the result to the database.
/// </summary>
public interface IInspiringCompanyFactionGenerator : ICompanyFactionGenerator
{
}
