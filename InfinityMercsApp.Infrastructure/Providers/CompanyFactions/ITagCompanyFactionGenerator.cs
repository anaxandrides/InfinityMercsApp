namespace InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

/// <summary>
/// Ensures the synthetic "TAG Company" faction (ID 2003) exists in the database.
/// The faction is created with metadata and an empty army payload until units are added later.
/// </summary>
public interface ITagCompanyFactionGenerator : ICompanyFactionGenerator
{
}
