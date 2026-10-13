using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;

namespace InfinityMercsApp.UnitTests.Storage;

/// <summary>
/// The import now parses a whole army payload in one typed pass. These tests prove that gives the same typed
/// models as the old route (cut the payload into JSON columns, parse each column later), using the raw
/// payload the reference database kept for every army.
/// </summary>
public sealed class ArmyPayloadEquivalenceTests
{
    private readonly ArmyJsonReader _reader = new();

    public static IEnumerable<object[]> Armies() =>
        TypedStorageRoundTripTests.ReadLegacyArmies()
            .Where(army => !string.IsNullOrWhiteSpace(army.RawJson))
            .Select(army => new object[] { army.FactionId });

    [Fact]
    public void NearlyEveryReferenceArmyKeptItsPayload()
    {
        // Army 2001 was stored without its raw payload; every other army has one to compare against.
        var withoutPayload = TypedStorageRoundTripTests.ReadLegacyArmies()
            .Where(army => string.IsNullOrWhiteSpace(army.RawJson))
            .Select(army => army.FactionId);
        Assert.Equal(new[] { 2001 }, withoutPayload);
    }

    [Theory]
    [MemberData(nameof(Armies))]
    public void PayloadArmyContent_MatchesTheColumns(int factionId)
    {
        var army = TypedStorageRoundTripTests.ReadLegacyArmies().Single(x => x.FactionId == factionId);
        var payload = _reader.ReadArmyPayload(army.RawJson);
        Assert.NotNull(payload);

        Assert.Equal(ModelDump.Of(_reader.ReadArmyFilters(army.FiltersJson)), ModelDump.Of(payload.Filters));
        Assert.Equal(ModelDump.Of(_reader.ReadRelations(army.RelationsJson)), ModelDump.Of(payload.Relations));
        Assert.Equal(ModelDump.Of(_reader.ReadFireteamChart(army.FireteamChartJson)), ModelDump.Of(payload.FireteamChart));
        Assert.Equal(_reader.ReadReinforcements(army.ReinforcementsJson), payload.ReinforcementsFactionId);
        Assert.Equal(ModelDump.Of(_reader.ReadSpecops(army.SpecopsJson)), ModelDump.Of(payload.Specops));
    }

    [Theory]
    [MemberData(nameof(Armies))]
    public void PayloadUnits_MatchTheUnitColumns(int factionId)
    {
        var army = TypedStorageRoundTripTests.ReadLegacyArmies().Single(x => x.FactionId == factionId);
        var payload = _reader.ReadArmyPayload(army.RawJson)!;

        var columns = TypedStorageRoundTripTests.ReadLegacyUnits()
            .Where(unit => unit.FactionId == factionId && unit.Key.Count(c => c == ':') == 3)
            .ToDictionary(unit => unit.Key, StringComparer.Ordinal);

        var payloadKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in payload.Units)
        {
            // The import's unit key: faction, unit id, army id, slug.
            var key = $"{factionId}:{unit.Id}:{unit.IdArmy ?? 0}:{unit.Slug ?? string.Empty}";
            payloadKeys.Add(key);
            Assert.True(columns.TryGetValue(key, out var stored), $"payload unit {key} is not in the units table");

            var fromColumns = new UnitContent(
                _reader.ReadProfileGroups(stored.ProfileGroups),
                _reader.ReadUnitOptions(stored.Options),
                _reader.ReadUnitFilters(stored.Filters),
                _reader.ReadFactionIds(stored.Factions));
            var fromPayload = new UnitContent(unit.ProfileGroups, unit.Options, unit.Filters, unit.FactionIds);
            Assert.Equal(ModelDump.Of(fromColumns), ModelDump.Of(fromPayload));
        }

        Assert.Equal(columns.Keys.Order(StringComparer.Ordinal), payloadKeys.Order(StringComparer.Ordinal));
    }
}
