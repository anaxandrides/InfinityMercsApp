using InfinityMercsApp.Infrastructure.Repositories;
using DbMetadataFaction = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Faction;

namespace InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

/// <summary>How a generated faction is listed with the metadata's factions: its name, slug and packaged logo.</summary>
public sealed record GeneratedFactionListing(int Id, string Name, string Slug, string Logo)
{
    /// <summary>Lists the faction, replacing any listing it had.</summary>
    public void StoreIn(ISQLiteRepository repository)
    {
        repository.Delete<DbMetadataFaction>(x => x.Id == Id);
        repository.Insert<DbMetadataFaction>(
        [
            new DbMetadataFaction
            {
                Id = Id,
                ParentId = Id,
                Name = Name,
                Slug = Slug,
                Discontinued = false,
                Logo = Logo
            }
        ]);
    }
}
