namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

public interface ICompanySourceFaction
{
    int Id { get; }
    int ParentId { get; }
    string Name { get; }
}
