using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Repositories;
using DbFaction = InfinityMercsApp.Infrastructure.Models.Database.Army.Faction;
using DbResume = InfinityMercsApp.Infrastructure.Models.Database.Army.Resume;
using DbSpecopsEquipment = InfinityMercsApp.Infrastructure.Models.Database.Army.SpecopsEquipment;
using DbSpecopsSkill = InfinityMercsApp.Infrastructure.Models.Database.Army.SpecopsSkill;
using DbSpecopsUnit = InfinityMercsApp.Infrastructure.Models.Database.Army.SpecopsUnit;
using DbSpecopsWeapon = InfinityMercsApp.Infrastructure.Models.Database.Army.SpecopsWeapon;
using DbUnit = InfinityMercsApp.Infrastructure.Models.Database.Army.Unit;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public class ArmyImportProvider(ISQLiteRepository sqliteRepository, IArmyContentStore contentStore) : IArmyImportProvider
{
    private const int YuJingFactionId = 201;
    private const int JsaFactionId = 1101;
    private const int JsaShindenbutaiFactionId = 1102;
    private const int JsaObanFactionId = 1103;

    /// <inheritdoc/>
    public Task ImportAsync(int factionId, ArmyDefinition army, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var faction = new DbFaction
        {
            FactionId = factionId,
            Version = army.Version,
            ImportedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ReinforcementsFactionId = army.ReinforcementsFactionId
        };

        var unitContent = new UnitContentRows();
        var units = new List<DbUnit>(army.Units.Count);
        foreach (var unit in army.Units)
        {
            var unitKey = BuildUnitKey(factionId, unit);
            units.Add(new DbUnit
            {
                UnitKey = unitKey,
                FactionId = factionId,
                UnitId = unit.Id,
                IdArmy = unit.IdArmy,
                Canonical = unit.Canonical,
                Isc = unit.Isc,
                IscAbbr = unit.IscAbbr,
                Name = unit.Name,
                Slug = unit.Slug
            });
            unitContent.Add(factionId, unitKey, ContentOf(unit));
        }

        var resume = army.Resume.Select(x => new DbResume
        {
            ResumeKey = BuildResumeKey(factionId, x),
            FactionId = factionId,
            UnitId = x.Id,
            IdArmy = x.IdArmy,
            Isc = x.Isc,
            Name = x.Name,
            Slug = x.Slug,
            Logo = x.Logo,
            Type = x.Type,
            Category = x.Category
        }).ToList();

        // JSA armies do not send their own spec-ops choices; they use Yu Jing's.
        var fallback = IsJsaFaction(factionId) ? ReadStoredSpecops(YuJingFactionId) : SpecopsCatalog.Empty;
        var specopsLinks = new SpecopsLinkRows();
        var specopsSkills = BuildChoices(factionId, army.Specops.Skills ?? fallback.Skills, specopsLinks, (key, order, choice) => new DbSpecopsSkill
        {
            SpecopsSkillKey = key,
            FactionId = factionId,
            EntryOrder = order,
            SkillId = choice.Id,
            Exp = choice.Exp
        });
        var specopsEquips = BuildChoices(factionId, army.Specops.Equip ?? fallback.Equip, specopsLinks, (key, order, choice) => new DbSpecopsEquipment
        {
            SpecopsEquipmentKey = key,
            FactionId = factionId,
            EntryOrder = order,
            EquipmentId = choice.Id,
            Exp = choice.Exp
        });
        var specopsWeapons = BuildChoices(factionId, army.Specops.Weapons ?? fallback.Weapons, specopsLinks, (key, order, choice) => new DbSpecopsWeapon
        {
            SpecopsWeaponKey = key,
            FactionId = factionId,
            EntryOrder = order,
            WeaponId = choice.Id,
            Exp = choice.Exp
        });

        var specopsUnits = new List<DbSpecopsUnit>(army.Specops.Units.Count);
        for (var entryOrder = 0; entryOrder < army.Specops.Units.Count; entryOrder++)
        {
            var unit = army.Specops.Units[entryOrder];
            var unitKey = $"{factionId}:{entryOrder}:{unit.Id}";
            specopsUnits.Add(new DbSpecopsUnit
            {
                SpecopsUnitKey = unitKey,
                FactionId = factionId,
                EntryOrder = entryOrder,
                UnitId = unit.Id,
                IdArmy = unit.IdArmy,
                Canonical = unit.Canonical,
                Isc = unit.Isc,
                IscAbbr = unit.IscAbbr,
                Name = unit.Name,
                Slug = unit.Slug
            });
            unitContent.Add(factionId, unitKey, ContentOf(unit));
        }

        var armyContent = new ArmyContentRows();
        armyContent.Add(factionId, new ArmyContent(army.Filters, army.Relations, army.FireteamChart));

        // One transaction per faction keeps the faction's rows atomic (no window where units are deleted
        // but not yet re-inserted) and commits once.
        sqliteRepository.RunInTransaction(() =>
        {
            sqliteRepository.Delete<DbUnit>(x => x.FactionId == factionId);
            sqliteRepository.Delete<DbResume>(x => x.FactionId == factionId);
            sqliteRepository.Delete<DbSpecopsSkill>(x => x.FactionId == factionId);
            sqliteRepository.Delete<DbSpecopsEquipment>(x => x.FactionId == factionId);
            sqliteRepository.Delete<DbSpecopsWeapon>(x => x.FactionId == factionId);
            sqliteRepository.Delete<DbSpecopsUnit>(x => x.FactionId == factionId);
            sqliteRepository.Delete<DbFaction>(x => x.FactionId == factionId);

            sqliteRepository.Insert([faction]);
            sqliteRepository.Insert(units);
            sqliteRepository.Insert(resume);
            sqliteRepository.Insert(specopsSkills);
            sqliteRepository.Insert(specopsEquips);
            sqliteRepository.Insert(specopsWeapons);
            sqliteRepository.Insert(specopsUnits);
            contentStore.ReplaceFactionContent(factionId, unitContent, armyContent, specopsLinks);
        });

        return Task.CompletedTask;
    }

    /// <summary>The import's unit key: faction, unit id, army id and slug.</summary>
    public static string BuildUnitKey(int factionId, ArmyUnitDefinition unit)
    {
        return $"{factionId}:{unit.Id}:{unit.IdArmy ?? 0}:{unit.Slug ?? string.Empty}";
    }

    private static string BuildResumeKey(int factionId, ArmyImportResume resume)
    {
        return $"{factionId}:{resume.Id}:{resume.Slug ?? string.Empty}";
    }

    private static bool IsJsaFaction(int factionId) =>
        factionId is JsaFactionId or JsaShindenbutaiFactionId or JsaObanFactionId;

    private static UnitContent ContentOf(ArmyUnitDefinition unit) =>
        new(unit.ProfileGroups, unit.Options, unit.Filters, unit.FactionIds);

    private static List<TRow> BuildChoices<TRow>(
        int factionId,
        IReadOnlyList<SpecopsChoice>? choices,
        SpecopsLinkRows links,
        Func<string, int, SpecopsChoice, TRow> createRow)
    {
        var rows = new List<TRow>();
        for (var entryOrder = 0; entryOrder < (choices?.Count ?? 0); entryOrder++)
        {
            var choice = choices![entryOrder];
            var key = $"{factionId}:{entryOrder}:{choice.Id}:{choice.Exp}";
            rows.Add(createRow(key, entryOrder, choice));
            links.Add(factionId, key, SpecopsChoiceLinks.From(choice));
        }

        return rows;
    }

    /// <summary>An army's stored spec-ops choices as a catalogue; lists are null when nothing is stored.</summary>
    private SpecopsCatalog ReadStoredSpecops(int factionId)
    {
        SpecopsChoice Choice(string key, int id, int exp)
        {
            var links = contentStore.GetSpecopsLinks(factionId, key);
            return new SpecopsChoice(id, exp, links.Extras, links.Skills, links.Equip, links.Weapons);
        }

        IReadOnlyList<SpecopsChoice>? NullIfEmpty(List<SpecopsChoice> choices) => choices.Count == 0 ? null : choices;

        return new SpecopsCatalog
        {
            Skills = NullIfEmpty(sqliteRepository.GetAll<DbSpecopsSkill>(x => x.FactionId == factionId, x => x.EntryOrder)
                .Select(row => Choice(row.SpecopsSkillKey, row.SkillId, row.Exp)).ToList()),
            Equip = NullIfEmpty(sqliteRepository.GetAll<DbSpecopsEquipment>(x => x.FactionId == factionId, x => x.EntryOrder)
                .Select(row => Choice(row.SpecopsEquipmentKey, row.EquipmentId, row.Exp)).ToList()),
            Weapons = NullIfEmpty(sqliteRepository.GetAll<DbSpecopsWeapon>(x => x.FactionId == factionId, x => x.EntryOrder)
                .Select(row => Choice(row.SpecopsWeaponKey, row.WeaponId, row.Exp)).ToList())
        };
    }
}
