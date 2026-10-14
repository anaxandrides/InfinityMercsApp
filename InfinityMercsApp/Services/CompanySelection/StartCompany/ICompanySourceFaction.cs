namespace InfinityMercsApp.Views.Common;

public interface ICompanySourceFaction
{
    int Id { get; }
    int ParentId { get; }
    string Name { get; }
}
