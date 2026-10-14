namespace InfinityMercsApp.Infrastructure.Providers;

using DomainMetadataDocument = InfinityMercsApp.Domain.Models.Metadata.MetadataDocument;
using DomainFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;
using DomainSkill = InfinityMercsApp.Domain.Models.Metadata.Skill;
using DomainWeapon = InfinityMercsApp.Domain.Models.Metadata.Weapon;
using DomainAmmunition = InfinityMercsApp.Domain.Models.Metadata.Ammunition;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.Models.Database.Metadata;
using InfinityMercsApp.Infrastructure.Repositories;
using DbAmmunition = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Ammunition;
using DbBooty = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Booty;
using DbEquipment = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Equipments;
using DbFaction = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Faction;
using DbHackProgram = InfinityMercsApp.Infrastructure.Models.Database.Metadata.HackingProgram;
using DbMartialArt = InfinityMercsApp.Infrastructure.Models.Database.Metadata.MartialArt;
using DbMetachemistry = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Metachemistry;
using DbSkill = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Skill;
using DbWeapon = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Weapon;

/// <inheritdoc/>
public sealed class MetadataProvider(ISQLiteRepository sqliteRepository) : IMetadataProvider
{
    /// <inheritdoc/>
    public void Import(DomainMetadataDocument metadata)
    {
        var factions = metadata.Factions.Select(x => new DbFaction
        {
            Id = x.Id,
            ParentId = x.ParentId,
            Name = x.Name,
            Slug = x.Slug,
            Discontinued = x.Discontinued,
            Logo = x.Logo
        }).ToList();

        var ammunitions = metadata.Ammunitions.Select(x => new DbAmmunition
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var weapons = metadata.Weapons.Select(x => new DbWeapon
        {
            WeaponKey = string.IsNullOrWhiteSpace(x.WeaponKey) ? BuildWeaponKey(x) : x.WeaponKey,
            WeaponId = x.WeaponId,
            Name = x.Name,
            Type = x.Type,
            Mode = x.Mode,
            Wiki = x.Wiki,
            AmmunitionId = x.AmmunitionId,
            Burst = x.Burst,
            Damage = x.Damage,
            Saving = x.Saving,
            SavingNum = x.SavingNum,
            Profile = x.Profile
        }).ToList();

        var weaponProperties = new List<WeaponPropertyRow>();
        var weaponBands = new List<WeaponDistanceBandRow>();
        for (var i = 0; i < metadata.Weapons.Count; i++)
        {
            var weapon = metadata.Weapons[i];
            var weaponKey = weapons[i].WeaponKey;
            weaponProperties.AddRange(weapon.Properties.Select((property, position) =>
                new WeaponPropertyRow { WeaponKey = weaponKey, Position = position, Property = property }));
            weaponBands.AddRange((weapon.Distance?.Bands ?? []).Select((band, position) =>
                new WeaponDistanceBandRow { WeaponKey = weaponKey, Position = position, Band = band.Name, Max = band.Max, Mod = band.Mod }));
        }

        var skills = metadata.Skills.Select(x => new DbSkill
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var equips = metadata.Equips.Select(x => new DbEquipment
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var hacks = metadata.Hack.Select(x => new DbHackProgram
        {
            Name = x.Name,
            Opponent = x.Opponent,
            Special = x.Special,
            Damage = x.Damage,
            Attack = x.Attack,
            Burst = x.Burst,
            Extra = x.Extra
        }).ToList();

        var martialArts = metadata.MartialArts.Select(x => new DbMartialArt
        {
            Name = x.Name,
            Opponent = x.Opponent,
            Damage = x.Damage,
            Attack = x.Attack,
            Burst = x.Burst
        }).ToList();

        var metachemistry = metadata.Metachemistry.Select(x => new DbMetachemistry
        {
            Id = x.Id,
            Name = x.Name,
            Value = x.Value
        }).ToList();

        var booty = metadata.Booty.Select(x => new DbBooty
        {
            Id = x.Id,
            Name = x.Name,
            Value = x.Value
        }).ToList();

        sqliteRepository.DeleteAll<DbFaction>();
        sqliteRepository.DeleteAll<DbAmmunition>();
        sqliteRepository.DeleteAll<DbWeapon>();
        sqliteRepository.DeleteAll<DbSkill>();
        sqliteRepository.DeleteAll<DbEquipment>();
        sqliteRepository.DeleteAll<DbHackProgram>();
        sqliteRepository.DeleteAll<DbMartialArt>();
        sqliteRepository.DeleteAll<DbMetachemistry>();
        sqliteRepository.DeleteAll<DbBooty>();
        sqliteRepository.DeleteAll<WeaponPropertyRow>();
        sqliteRepository.DeleteAll<WeaponDistanceBandRow>();
        sqliteRepository.DeleteAll<HackingProgramValueRow>();

        sqliteRepository.Insert(factions);
        sqliteRepository.Insert(ammunitions);
        sqliteRepository.Insert(weapons);
        sqliteRepository.Insert(skills);
        sqliteRepository.Insert(equips);
        sqliteRepository.Insert(hacks);
        sqliteRepository.Insert(martialArts);
        sqliteRepository.Insert(metachemistry);
        sqliteRepository.Insert(booty);
        sqliteRepository.Insert(weaponProperties);
        sqliteRepository.Insert(weaponBands);

        // Inserting assigned each program its AutoIncrement id, which its list values refer to.
        var hackValues = new List<HackingProgramValueRow>();
        for (var i = 0; i < metadata.Hack.Count; i++)
        {
            var program = metadata.Hack[i];
            var programId = hacks[i].Id;
            hackValues.AddRange(program.SkillTypes.Select((text, position) =>
                new HackingProgramValueRow { ProgramId = programId, ListKind = HackingProgramLists.SkillType, Position = position, Text = text }));
            hackValues.AddRange(program.Devices.Select((device, position) =>
                new HackingProgramValueRow { ProgramId = programId, ListKind = HackingProgramLists.Devices, Position = position, Number = device }));
            hackValues.AddRange(program.Targets.Select((text, position) =>
                new HackingProgramValueRow { ProgramId = programId, ListKind = HackingProgramLists.Target, Position = position, Text = text }));
        }

        sqliteRepository.Insert(hackValues);
    }

    /// <inheritdoc/>
    public bool HasMetadata()
    {
        return sqliteRepository.Exists<DbFaction>(x => true);
    }

    /// <inheritdoc/>
    public IReadOnlyList<DomainFaction> GetFactions(bool includeDiscontinued = false)
    {
        var records = includeDiscontinued
            ? sqliteRepository.GetAll<DbFaction>(x => true, x => x.Name)
            : sqliteRepository.GetAll<DbFaction>(x => !x.Discontinued, x => x.Name);

        return records.Select(MapFaction).ToList();
    }

    /// <inheritdoc/>
    public DomainFaction? GetFactionById(int id)
    {
        var row = sqliteRepository.GetById<DbFaction>(id);
        return row is null ? null : MapFaction(row);
    }

    /// <inheritdoc/>
    public IReadOnlyList<DomainWeapon> GetAllWeapons()
    {
        return MapWeapons(sqliteRepository.GetAll<DbWeapon>(x => true, x => x.Name));
    }

    /// <inheritdoc/>
    public IReadOnlyList<DomainWeapon> SearchWeaponsByName(string searchTerm)
    {
        var records = string.IsNullOrWhiteSpace(searchTerm)
            ? sqliteRepository.GetAll<DbWeapon>(x => true, x => x.Name, 100)
            : sqliteRepository.GetAll<DbWeapon>(x => x.Name.Contains(searchTerm), x => x.Name, 100);
        return MapWeapons(records);
    }

    /// <inheritdoc/>
    public IReadOnlyList<DomainSkill> GetSkills()
    {
        return sqliteRepository.GetAll<DbSkill>(x => true, x => x.Name)
            .Select(MapSkill)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<DomainAmmunition> GetAmmunitions()
    {
        return sqliteRepository.GetAll<DbAmmunition>(x => true, x => x.Name)
            .Select(MapAmmunition)
            .ToList();
    }

    private static DomainFaction MapFaction(DbFaction source)
    {
        return new DomainFaction
        {
            Id = source.Id,
            ParentId = source.ParentId,
            Name = source.Name,
            Slug = source.Slug,
            Discontinued = source.Discontinued,
            Logo = source.Logo
        };
    }

    private IReadOnlyList<DomainWeapon> MapWeapons(IReadOnlyList<DbWeapon> records)
    {
        // The weapon list is small (a few hundred rows), so its property and band rows are read in full.
        var properties = sqliteRepository.GetAll<WeaponPropertyRow>(x => true).ToLookup(row => row.WeaponKey);
        var bands = sqliteRepository.GetAll<WeaponDistanceBandRow>(x => true).ToLookup(row => row.WeaponKey);
        return records.Select(record => MapWeapon(record, properties[record.WeaponKey], bands[record.WeaponKey])).ToList();
    }

    private static DomainWeapon MapWeapon(DbWeapon source, IEnumerable<WeaponPropertyRow> properties, IEnumerable<WeaponDistanceBandRow> bands)
    {
        var bandList = bands
            .OrderBy(row => row.Position)
            .Select(row => new WeaponDistanceBand { Name = row.Band, Max = row.Max, Mod = row.Mod })
            .ToList();
        return new DomainWeapon
        {
            WeaponKey = source.WeaponKey,
            WeaponId = source.WeaponId,
            Name = source.Name,
            Type = source.Type,
            Mode = source.Mode,
            Wiki = source.Wiki,
            AmmunitionId = source.AmmunitionId,
            Burst = source.Burst,
            Damage = source.Damage,
            Saving = source.Saving,
            SavingNum = source.SavingNum,
            Profile = source.Profile,
            Properties = properties.OrderBy(row => row.Position).Select(row => row.Property).ToList(),
            Distance = bandList.Count == 0 ? null : new WeaponDistance { Bands = bandList }
        };
    }

    private static DomainSkill MapSkill(DbSkill source)
    {
        return new DomainSkill
        {
            Id = source.Id,
            Name = source.Name,
            Wiki = source.Wiki
        };
    }

    private static DomainAmmunition MapAmmunition(DbAmmunition source)
    {
        return new DomainAmmunition
        {
            Id = source.Id,
            Name = source.Name,
            Wiki = source.Wiki
        };
    }

    private static string BuildWeaponKey(DomainWeapon weapon)
    {
        return $"{weapon.WeaponId}:{weapon.Name}:{weapon.Mode ?? string.Empty}";
    }
}
