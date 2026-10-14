using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Models.Database.Army;

namespace InfinityMercsApp.Infrastructure.Data;

/// <summary>
/// The rows of the army content tables, and the mapping between them and <see cref="ArmyContent"/>.
/// </summary>
public sealed class ArmyContentRows
{
    public List<ArmyFilterItemRow> FilterItems { get; } = [];

    public List<ArmyRelationRow> Relations { get; } = [];

    public List<ArmyRelationUnitRow> RelationUnits { get; } = [];

    public List<FireteamChartSpecRow> Specs { get; } = [];

    public List<FireteamChartTeamRow> Teams { get; } = [];

    public List<FireteamChartTeamTypeRow> TeamTypes { get; } = [];

    public List<FireteamChartTeamUnitRow> TeamUnits { get; } = [];

    public void Add(int factionId, ArmyContent content)
    {
        AddFilters(factionId, content.Filters);
        AddRelations(factionId, content.Relations);
        AddFireteamChart(factionId, content.FireteamChart);
    }

    /// <summary>
    /// Rebuilds the content of every army the rows describe. Armies listed in <paramref name="factionIds"/>
    /// without any content rows get <see cref="ArmyContent.Empty"/>.
    /// </summary>
    public Dictionary<int, ArmyContent> Assemble(IEnumerable<int>? factionIds = null)
    {
        var filterRows = FilterItems.ToLookup(row => row.FactionId);
        var relationRows = Relations.ToLookup(row => row.FactionId);
        var relationUnitRows = RelationUnits.ToLookup(row => row.FactionId);
        var specRows = Specs.ToLookup(row => row.FactionId);
        var teamRows = Teams.ToLookup(row => row.FactionId);
        var teamTypeRows = TeamTypes.ToLookup(row => row.FactionId);
        var teamUnitRows = TeamUnits.ToLookup(row => row.FactionId);

        var allFactionIds = new HashSet<int>(factionIds ?? []);
        allFactionIds.UnionWith(FilterItems.Select(row => row.FactionId));
        allFactionIds.UnionWith(Relations.Select(row => row.FactionId));
        allFactionIds.UnionWith(Specs.Select(row => row.FactionId));
        allFactionIds.UnionWith(Teams.Select(row => row.FactionId));

        var result = new Dictionary<int, ArmyContent>(allFactionIds.Count);
        foreach (var factionId in allFactionIds)
        {
            result[factionId] = new ArmyContent(
                BuildFilters(filterRows[factionId]),
                BuildRelations(relationRows[factionId], relationUnitRows[factionId]),
                BuildFireteamChart(specRows[factionId], teamRows[factionId], teamTypeRows[factionId], teamUnitRows[factionId]));
        }

        return result;
    }

    private void AddFilters(int factionId, ArmyFilterCatalog filters)
    {
        // A section can hold numeric-id items, text-id items (see ArmyFilterKeyedItem) or both.
        var sectionIndex = 0;
        foreach (var section in filters.Sections)
        {
            var items = filters.Items(section);
            for (var position = 0; position < items.Count; position++)
            {
                var item = items[position];
                FilterItems.Add(new ArmyFilterItemRow
                {
                    FactionId = factionId,
                    Section = section,
                    SectionIndex = sectionIndex,
                    Position = position,
                    ItemId = item.Id,
                    Name = item.Name,
                    Type = item.Type,
                    Url = item.Url,
                    Mercs = item.Mercs,
                    SpecOps = item.SpecOps
                });
            }

            var keyedItems = filters.KeyedItems(section);
            for (var position = 0; position < keyedItems.Count; position++)
            {
                var item = keyedItems[position];
                FilterItems.Add(new ArmyFilterItemRow
                {
                    FactionId = factionId,
                    Section = section,
                    SectionIndex = sectionIndex,
                    Position = position,
                    ItemKey = item.Key,
                    Name = item.Name,
                    Abbr = item.Abbr,
                    Min = item.Min,
                    Max = item.Max
                });
            }

            if (items.Count == 0 && keyedItems.Count == 0)
            {
                // Keep empty sections so the catalogue lists the same sections it was built with.
                FilterItems.Add(new ArmyFilterItemRow { FactionId = factionId, Section = section, SectionIndex = sectionIndex, Position = -1 });
            }

            sectionIndex++;
        }
    }

    private void AddRelations(int factionId, IReadOnlyList<ArmyRelation> relations)
    {
        for (var relationIndex = 0; relationIndex < relations.Count; relationIndex++)
        {
            var relation = relations[relationIndex];
            Relations.Add(new ArmyRelationRow
            {
                FactionId = factionId,
                RelationIndex = relationIndex,
                Min = relation.Min,
                Max = relation.Max,
                IsGroup = relation.IsGroup
            });

            for (var position = 0; position < relation.UnitIds.Count; position++)
            {
                RelationUnits.Add(new ArmyRelationUnitRow
                {
                    FactionId = factionId,
                    RelationIndex = relationIndex,
                    Position = position,
                    UnitId = relation.UnitIds[position]
                });
            }
        }
    }

    private void AddFireteamChart(int factionId, FireteamChart chart)
    {
        for (var position = 0; position < chart.Spec.Count; position++)
        {
            var spec = chart.Spec[position];
            Specs.Add(new FireteamChartSpecRow { FactionId = factionId, Position = position, Key = spec.Key, Value = spec.Count });
        }

        for (var teamIndex = 0; teamIndex < chart.Teams.Count; teamIndex++)
        {
            var team = chart.Teams[teamIndex];
            Teams.Add(new FireteamChartTeamRow
            {
                FactionId = factionId,
                TeamIndex = teamIndex,
                Name = team.Name,
                HasTypeList = team.TypeList is not null,
                TypeText = team.TypeText
            });

            var types = team.TypeList ?? [];
            for (var position = 0; position < types.Count; position++)
            {
                TeamTypes.Add(new FireteamChartTeamTypeRow { FactionId = factionId, TeamIndex = teamIndex, Position = position, Type = types[position] });
            }

            for (var position = 0; position < team.Units.Count; position++)
            {
                var unit = team.Units[position];
                TeamUnits.Add(new FireteamChartTeamUnitRow
                {
                    FactionId = factionId,
                    TeamIndex = teamIndex,
                    Position = position,
                    Name = unit.Name,
                    Slug = unit.Slug,
                    Comment = unit.Comment,
                    Min = unit.Min,
                    Max = unit.Max,
                    MinIsAsterisk = unit.MinIsAsterisk,
                    Required = unit.Required
                });
            }
        }
    }

    private static ArmyFilterCatalog BuildFilters(IEnumerable<ArmyFilterItemRow> rows)
    {
        var sections = new Dictionary<string, IReadOnlyList<ArmyFilterItem>>(StringComparer.Ordinal);
        var keyedSections = new Dictionary<string, IReadOnlyList<ArmyFilterKeyedItem>>(StringComparer.Ordinal);

        foreach (var section in rows.GroupBy(row => (row.SectionIndex, row.Section)).OrderBy(group => group.Key.SectionIndex))
        {
            var ordered = section.Where(row => row.Position >= 0).OrderBy(row => row.Position).ToList();
            sections[section.Key.Section] = ordered
                .Where(row => row.ItemId is not null)
                .Select(row => new ArmyFilterItem(row.ItemId!.Value, row.Name, row.Type, row.Url) { Mercs = row.Mercs, SpecOps = row.SpecOps })
                .ToList();

            var keyed = ordered
                .Where(row => row.ItemId is null && row.ItemKey is not null)
                .Select(row => new ArmyFilterKeyedItem(row.ItemKey!, row.Name, row.Abbr, row.Min, row.Max))
                .ToList();
            if (keyed.Count > 0)
            {
                keyedSections[section.Key.Section] = keyed;
            }
        }

        return sections.Count == 0 ? ArmyFilterCatalog.Empty : new ArmyFilterCatalog(sections, keyedSections);
    }

    private static IReadOnlyList<ArmyRelation> BuildRelations(IEnumerable<ArmyRelationRow> relations, IEnumerable<ArmyRelationUnitRow> units)
    {
        var unitsByRelation = units.ToLookup(row => row.RelationIndex);
        return relations
            .OrderBy(row => row.RelationIndex)
            .Select(row => new ArmyRelation(
                row.Min,
                row.Max,
                row.IsGroup,
                unitsByRelation[row.RelationIndex].OrderBy(unit => unit.Position).Select(unit => unit.UnitId).ToList()))
            .ToList();
    }

    private static FireteamChart BuildFireteamChart(
        IEnumerable<FireteamChartSpecRow> specs,
        IEnumerable<FireteamChartTeamRow> teams,
        IEnumerable<FireteamChartTeamTypeRow> teamTypes,
        IEnumerable<FireteamChartTeamUnitRow> teamUnits)
    {
        var specList = specs.OrderBy(row => row.Position).Select(row => new FireteamSpecEntry(row.Key, row.Value)).ToList();
        var teamList = teams.OrderBy(row => row.TeamIndex).ToList();
        if (specList.Count == 0 && teamList.Count == 0)
        {
            return FireteamChart.Empty;
        }

        var typesByTeam = teamTypes.ToLookup(row => row.TeamIndex);
        var unitsByTeam = teamUnits.ToLookup(row => row.TeamIndex);
        return new FireteamChart
        {
            Spec = specList,
            Teams = teamList.Select(team => new FireteamTeam
            {
                Name = team.Name,
                TypeList = team.HasTypeList
                    ? typesByTeam[team.TeamIndex].OrderBy(row => row.Position).Select(row => row.Type).ToList()
                    : null,
                TypeText = team.TypeText,
                Units = unitsByTeam[team.TeamIndex].OrderBy(row => row.Position).Select(row => new FireteamUnitLimit
                {
                    Name = row.Name,
                    Slug = row.Slug,
                    Comment = row.Comment,
                    Min = row.Min,
                    Max = row.Max,
                    MinIsAsterisk = row.MinIsAsterisk,
                    Required = row.Required
                }).ToList()
            }).ToList()
        };
    }
}
