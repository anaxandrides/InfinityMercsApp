using System.Collections.Concurrent;
using InfinityMercsApp.Infrastructure.Models.Database.Army;
using InfinityMercsApp.Infrastructure.Models.Database.Army.Fireteams;
using InfinityMercsApp.Infrastructure.Models.Database.Army.SpecOps;
using InfinityMercsApp.Infrastructure.Models.Database.Army.Units;
using InfinityMercsApp.Infrastructure.Repositories;

namespace InfinityMercsApp.Infrastructure.Data;

/// <inheritdoc/>
public sealed class ArmyContentStore(ISQLiteRepository repository) : IArmyContentStore
{
    private readonly ConcurrentDictionary<int, Lazy<FactionContent>> _cache = new();

    public ArmyContent GetArmyContent(int factionId) => Load(factionId).Army;

    public UnitContent GetUnitContent(int factionId, string ownerKey) =>
        Load(factionId).Units.TryGetValue(ownerKey, out var content) ? content : UnitContent.Empty;

    public SpecopsChoiceLinks GetSpecopsLinks(int factionId, string ownerKey) =>
        Load(factionId).SpecopsLinks.TryGetValue(ownerKey, out var links) ? links : SpecopsChoiceLinks.Empty;

    public void ReplaceFactionContent(int factionId, UnitContentRows units, ArmyContentRows army, SpecopsLinkRows specopsLinks)
    {
        repository.Delete<UnitProfileGroupRow>(x => x.FactionId == factionId);
        repository.Delete<UnitProfileRow>(x => x.FactionId == factionId);
        repository.Delete<UnitProfileMoveRow>(x => x.FactionId == factionId);
        repository.Delete<UnitOptionRow>(x => x.FactionId == factionId);
        repository.Delete<UnitOptionOrderRow>(x => x.FactionId == factionId);
        repository.Delete<UnitOptionIncludeRow>(x => x.FactionId == factionId);
        repository.Delete<UnitEntryRow>(x => x.FactionId == factionId);
        repository.Delete<UnitEntryExtraRow>(x => x.FactionId == factionId);
        repository.Delete<UnitFilterIdRow>(x => x.FactionId == factionId);
        repository.Delete<UnitFactionRow>(x => x.FactionId == factionId);
        repository.Delete<ArmyFilterItemRow>(x => x.FactionId == factionId);
        repository.Delete<ArmyRelationRow>(x => x.FactionId == factionId);
        repository.Delete<ArmyRelationUnitRow>(x => x.FactionId == factionId);
        repository.Delete<FireteamChartSpecRow>(x => x.FactionId == factionId);
        repository.Delete<FireteamChartTeamRow>(x => x.FactionId == factionId);
        repository.Delete<FireteamChartTeamTypeRow>(x => x.FactionId == factionId);
        repository.Delete<FireteamChartTeamUnitRow>(x => x.FactionId == factionId);
        repository.Delete<SpecopsLinkRow>(x => x.FactionId == factionId);
        repository.Delete<SpecopsLinkExtraRow>(x => x.FactionId == factionId);

        repository.Insert(units.Groups);
        repository.Insert(units.Profiles);
        repository.Insert(units.Moves);
        repository.Insert(units.Options);
        repository.Insert(units.Orders);
        repository.Insert(units.Includes);
        repository.Insert(units.Entries);
        repository.Insert(units.Extras);
        repository.Insert(units.FilterIds);
        repository.Insert(units.Factions);
        repository.Insert(army.FilterItems);
        repository.Insert(army.Relations);
        repository.Insert(army.RelationUnits);
        repository.Insert(army.Specs);
        repository.Insert(army.Teams);
        repository.Insert(army.TeamTypes);
        repository.Insert(army.TeamUnits);
        repository.Insert(specopsLinks.Links);
        repository.Insert(specopsLinks.LinkExtras);

        _cache.TryRemove(factionId, out _);
    }

    private FactionContent Load(int factionId) =>
        _cache.GetOrAdd(factionId, id => new Lazy<FactionContent>(() => Read(id))).Value;

    private FactionContent Read(int factionId)
    {
        var units = new UnitContentRows();
        units.Groups.AddRange(repository.GetAll<UnitProfileGroupRow>(x => x.FactionId == factionId));
        units.Profiles.AddRange(repository.GetAll<UnitProfileRow>(x => x.FactionId == factionId));
        units.Moves.AddRange(repository.GetAll<UnitProfileMoveRow>(x => x.FactionId == factionId));
        units.Options.AddRange(repository.GetAll<UnitOptionRow>(x => x.FactionId == factionId));
        units.Orders.AddRange(repository.GetAll<UnitOptionOrderRow>(x => x.FactionId == factionId));
        units.Includes.AddRange(repository.GetAll<UnitOptionIncludeRow>(x => x.FactionId == factionId));
        units.Entries.AddRange(repository.GetAll<UnitEntryRow>(x => x.FactionId == factionId));
        units.Extras.AddRange(repository.GetAll<UnitEntryExtraRow>(x => x.FactionId == factionId));
        units.FilterIds.AddRange(repository.GetAll<UnitFilterIdRow>(x => x.FactionId == factionId));
        units.Factions.AddRange(repository.GetAll<UnitFactionRow>(x => x.FactionId == factionId));

        var army = new ArmyContentRows();
        army.FilterItems.AddRange(repository.GetAll<ArmyFilterItemRow>(x => x.FactionId == factionId));
        army.Relations.AddRange(repository.GetAll<ArmyRelationRow>(x => x.FactionId == factionId));
        army.RelationUnits.AddRange(repository.GetAll<ArmyRelationUnitRow>(x => x.FactionId == factionId));
        army.Specs.AddRange(repository.GetAll<FireteamChartSpecRow>(x => x.FactionId == factionId));
        army.Teams.AddRange(repository.GetAll<FireteamChartTeamRow>(x => x.FactionId == factionId));
        army.TeamTypes.AddRange(repository.GetAll<FireteamChartTeamTypeRow>(x => x.FactionId == factionId));
        army.TeamUnits.AddRange(repository.GetAll<FireteamChartTeamUnitRow>(x => x.FactionId == factionId));

        var links = new SpecopsLinkRows();
        links.Links.AddRange(repository.GetAll<SpecopsLinkRow>(x => x.FactionId == factionId));
        links.LinkExtras.AddRange(repository.GetAll<SpecopsLinkExtraRow>(x => x.FactionId == factionId));

        return new FactionContent(
            units.Assemble(),
            army.Assemble([factionId])[factionId],
            links.Assemble());
    }

    private sealed record FactionContent(
        Dictionary<string, UnitContent> Units,
        ArmyContent Army,
        Dictionary<string, SpecopsChoiceLinks> SpecopsLinks);
}
