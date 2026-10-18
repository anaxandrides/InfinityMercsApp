using InfinityMercsApp.Domain.Models.Perks.Catalog;

namespace InfinityMercsApp.UnitTests.Perks;

public sealed class CompanyPerkCatalogPerkNodeTests
{
    [Fact]
    public void GetPerkNodeLists_Catalog_BuildsRootsForEveryPerkList()
    {
        var lists = CompanyPerkCatalog.GetPerkNodeLists();
        var catalogLists = CompanyPerkCatalog.GetPerkListCatalogEntries();

        Assert.Equal(catalogLists.Count, lists.Count);
        Assert.All(lists, list =>
        {
            Assert.False(string.IsNullOrWhiteSpace(list.ListId));
            Assert.False(string.IsNullOrWhiteSpace(list.ListName));
            Assert.NotEmpty(list.Roots);
        });
    }
}
