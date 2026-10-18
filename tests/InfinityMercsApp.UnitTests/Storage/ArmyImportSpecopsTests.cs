using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Army.SpecOps;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Options;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Repositories;
using InfinityMercsApp.UnitTests.Characterization;
using Microsoft.Data.Sqlite;

namespace InfinityMercsApp.UnitTests.Storage;

/// <summary>
/// Corvus Belli's army data stopped sending spec-ops in October 2026. Importing an army without them keeps the
/// spec-ops choices and units already stored, so the Mercs Spec-Ops rules still have their XP costs.
/// </summary>
public sealed class ArmyImportSpecopsTests : IDisposable
{
    private const int PanOceania = 101;

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"army-import-{Guid.NewGuid():N}.db3");
    private readonly SQLiteRepository _repository;
    private readonly ArmyContentStore _contentStore;
    private readonly ArmyImportProvider _import;
    private readonly SpecOpsProvider _specops;
    private readonly ArmyDefinition _army;

    public ArmyImportSpecopsTests()
    {
        _repository = new SQLiteRepository(new SQLIteConfiguration { DBPath = _databasePath });
        _contentStore = new ArmyContentStore(_repository);
        _import = new ArmyImportProvider(_repository, _contentStore);
        _specops = new SpecOpsProvider(_repository, _contentStore);
        _army = new ArmyJsonReader().ReadArmyPayload(File.ReadAllText(RepoFiles.Resolve(ReferenceCorpus.Army101Path)))
                ?? throw new InvalidOperationException($"{ReferenceCorpus.Army101Path} could not be read.");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        IEnumerable<string> suffixes = [string.Empty, "-wal", "-shm"];
        foreach (var suffix in suffixes)
        {
            try
            {
                File.Delete(_databasePath + suffix);
            }
            catch (IOException)
            {
                // The repository keeps its connection open; a leftover temp file is harmless.
            }
        }
    }

    [Fact]
    public void Import_ReferenceArmy_StoresSpecops()
    {
        _import.Import(PanOceania, _army);

        Assert.NotEmpty(_specops.GetSpecopsSkillsByFaction(PanOceania));
        Assert.NotEmpty(_specops.GetSpecopsEquipsByFaction(PanOceania));
        Assert.NotEmpty(_specops.GetSpecopsWeaponsByFaction(PanOceania));
        Assert.NotEmpty(_specops.GetSpecopsUnitsByFaction(PanOceania));
    }

    [Fact]
    public void Import_WithoutSpecops_KeepsTheStoredSpecops()
    {
        _import.Import(PanOceania, _army);
        var before = StoredSpecops();

        _import.Import(PanOceania, WithSpecops(_army, SpecopsCatalog.Empty));

        Assert.Equal(before, StoredSpecops());
    }

    [Fact]
    public void Import_WithoutSpecops_KeepsTheSpecopsUnitProfiles()
    {
        _import.Import(PanOceania, _army);
        var before = _specops.GetSpecopsUnitsByFaction(PanOceania).Select(unit => ModelDump.Of(unit.ProfileGroups)).ToList();

        _import.Import(PanOceania, WithSpecops(_army, SpecopsCatalog.Empty));

        var after = _specops.GetSpecopsUnitsByFaction(PanOceania).Select(unit => ModelDump.Of(unit.ProfileGroups)).ToList();
        Assert.Equal(before, after);
        Assert.DoesNotContain(ModelDump.Of(UnitProfileGroups.Empty), after);
    }

    [Fact]
    public void Import_WithSpecops_ReplacesTheStoredSpecops()
    {
        _import.Import(PanOceania, _army);
        var firstSkill = _army.Specops.Skills?.First() ?? throw new InvalidOperationException("The reference army has no spec-ops skills.");

        _import.Import(PanOceania, WithSpecops(_army, new SpecopsCatalog { Skills = [firstSkill] }));

        Assert.Equal([firstSkill.Id], _specops.GetSpecopsSkillsByFaction(PanOceania).Select(skill => skill.SkillId));
        Assert.Empty(_specops.GetSpecopsEquipsByFaction(PanOceania));
        Assert.Empty(_specops.GetSpecopsUnitsByFaction(PanOceania));
    }

    [Fact]
    public void Import_WithoutSpecops_StillReplacesTheUnits()
    {
        _import.Import(PanOceania, _army);
        var oneUnit = new ArmyDefinition
        {
            Version = _army.Version,
            Units = [_army.Units[0]],
            Resume = [_army.Resume[0]],
            Filters = _army.Filters,
            Relations = _army.Relations,
            FireteamChart = _army.FireteamChart,
            Specops = SpecopsCatalog.Empty
        };

        _import.Import(PanOceania, oneUnit);

        var unitKey = ArmyImportProvider.BuildUnitKey(PanOceania, _army.Units[0]);
        Assert.Equal(
            ModelDump.Of(_contentStore.GetUnitContent(PanOceania, unitKey).ProfileGroups),
            ModelDump.Of(_army.Units[0].ProfileGroups));
        Assert.Equal(
            ModelDump.Of(UnitContent.Empty),
            ModelDump.Of(_contentStore.GetUnitContent(PanOceania, ArmyImportProvider.BuildUnitKey(PanOceania, _army.Units[1]))));
    }

    private string StoredSpecops() => ModelDump.Of(new StoredSpecopsDump(
        _specops.GetSpecopsSkillsByFaction(PanOceania),
        _specops.GetSpecopsEquipsByFaction(PanOceania),
        _specops.GetSpecopsWeaponsByFaction(PanOceania),
        _specops.GetSpecopsUnitsByFaction(PanOceania)));

    private static ArmyDefinition WithSpecops(ArmyDefinition army, SpecopsCatalog specops) => new()
    {
        Version = army.Version,
        Units = army.Units,
        Resume = army.Resume,
        ReinforcementsFactionId = army.ReinforcementsFactionId,
        Filters = army.Filters,
        Relations = army.Relations,
        FireteamChart = army.FireteamChart,
        Specops = specops
    };
}
