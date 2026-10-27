using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Services.ArmyData;
using NSubstitute;
using ArmyFaction = InfinityMercsApp.Domain.Models.Army.Faction;
using MetadataFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.UnitTests.Services.ArmyData;

/// <summary>The army data service hands out copies of what the providers hold, and nothing when they are missing.</summary>
public sealed class ArmyDataServiceTests
{
    private readonly IMetadataProvider _metadata = Substitute.For<IMetadataProvider>();
    private readonly IFactionProvider _factions = Substitute.For<IFactionProvider>();
    private readonly ICohesiveCompanyFactionQueryProvider _cohesive = Substitute.For<ICohesiveCompanyFactionQueryProvider>();
    private readonly IAppSettingsProvider _settings = Substitute.For<IAppSettingsProvider>();

    private ArmyDataService Service => new(_metadata, _factions, _cohesive, _settings);

    private ArmyDataService WithoutProviders => new(null, null, _cohesive, null);

    [Fact]
    public void GetMetadataFactions_Stored_ReturnsCopies()
    {
        var stored = new MetadataFaction { Id = 101, ParentId = 100, Name = "PanO", Slug = "pano", Logo = "logo" };
        _metadata.GetFactions(true).Returns([stored]);

        var faction = Assert.Single(Service.GetMetadataFactions(includeDiscontinued: true));

        Assert.NotSame(stored, faction);
        Assert.Equal("PanO", faction.Name);
        Assert.Equal(100, faction.ParentId);
        Assert.Equal("logo", faction.Logo);
    }

    [Fact]
    public void GetMetadataFactionById_Stored_ReturnsACopy()
    {
        var stored = new MetadataFaction { Id = 101, Name = "PanO" };
        _metadata.GetFactionById(101).Returns(stored);

        var faction = Service.GetMetadataFactionById(101);

        Assert.NotSame(stored, faction);
        Assert.Equal("PanO", faction?.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(102)]
    public void GetMetadataFactionById_NoSuchFaction_IsNull(int id) => Assert.Null(Service.GetMetadataFactionById(id));

    [Fact]
    public void GetFactionSnapshot_Stored_ReturnsACopy()
    {
        var stored = new ArmyFaction { FactionId = 101, Version = "7", ReinforcementsFactionId = 1001 };
        _factions.GetFactionSnapshot(101).Returns(stored);

        var snapshot = Service.GetFactionSnapshot(101);

        Assert.NotSame(stored, snapshot);
        Assert.Equal("7", snapshot?.Version);
        Assert.Equal(1001, snapshot?.ReinforcementsFactionId);
        Assert.Null(Service.GetFactionSnapshot(102));
    }

    [Fact]
    public void GetResumes_Stored_ReturnCopies()
    {
        var resume = new Resume { FactionId = 101, UnitId = 5, Name = "Fusilier", Isc = "FUS", Type = 1 };
        _factions.GetResumeByFaction(101).Returns([resume]);
        _factions.GetResumeByFactionMercsOnly(101).Returns([resume]);

        var all = Assert.Single(Service.GetResumeByFaction(101));
        var mercs = Assert.Single(Service.GetResumeByFactionMercsOnly(101));

        Assert.NotSame(resume, all);
        Assert.NotSame(resume, mercs);
        Assert.Equal("Fusilier", all.Name);
        Assert.Equal("FUS", mercs.Isc);
        Assert.Equal(1, all.Type);
    }

    [Fact]
    public void GetUnit_Stored_ReturnsACopy()
    {
        var stored = new Unit { FactionId = 101, UnitId = 5, Name = "Fusilier", Isc = "FUS", FactionIds = [101] };
        _factions.GetUnit(101, 5).Returns(stored);

        var unit = Service.GetUnit(101, 5);

        Assert.NotSame(stored, unit);
        Assert.Equal("Fusilier", unit?.Name);
        Assert.Equal("FUS", unit?.Isc);
        Assert.Equal([101], unit?.FactionIds);
        Assert.Null(Service.GetUnit(101, 6));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(101, 0)]
    public void GetUnit_NoIds_IsNullWithoutAsking(int factionId, int unitId)
    {
        Assert.Null(Service.GetUnit(factionId, unitId));
        _factions.DidNotReceiveWithAnyArgs().GetUnit(default, default);
    }

    [Fact]
    public void Readers_NoProviders_ReturnNothing()
    {
        var service = WithoutProviders;

        Assert.Empty(service.GetMetadataFactions());
        Assert.Null(service.GetMetadataFactionById(101));
        Assert.Null(service.GetFactionSnapshot(101));
        Assert.Empty(service.GetResumeByFaction(101));
        Assert.Empty(service.GetResumeByFactionMercsOnly(101));
        Assert.Null(service.GetUnit(101, 5));
        Assert.Empty(service.GetRecruitTable(RecruitRollTable.Booty));
        Assert.Empty(service.GetWeaponNames());
        Assert.Empty(service.GetSkillNames());
    }

    [Fact]
    public void Readers_NoFactionId_ReturnNothing()
    {
        Assert.Null(Service.GetFactionSnapshot(0));
        Assert.Empty(Service.GetResumeByFaction(0));
        Assert.Empty(Service.GetResumeByFactionMercsOnly(-1));
    }

    [Fact]
    public void Readers_Cancelled_Throw()
    {
        var cancelled = new CancellationToken(true);

        Assert.Throws<OperationCanceledException>(() => Service.GetMetadataFactions(cancellationToken: cancelled));
        Assert.Throws<OperationCanceledException>(() => Service.GetUnit(101, 5, cancelled));
    }

    [Fact]
    public void GetMergedMercsArmyList_NoFactions_IsEmptyWithoutAsking()
    {
        Assert.Empty(Service.GetMergedMercsArmyList([]));
        _cohesive.DidNotReceive().GetFilterQuerySource(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetMergedMercsArmyList_Factions_ReturnsTheMergedEntries()
    {
        var entry = new MercsArmyListEntry { Resume = new Resume { Name = "Fusilier" } };
        _cohesive.GetFilterQuerySource(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new CohesiveCompanyFactionQueryResult { MergedMercsListEntries = [entry] });

        Assert.Same(entry, Assert.Single(Service.GetMergedMercsArmyList([101, 102])));
    }

    [Fact]
    public void GetRecruitTable_Booty_ParsesAndOrdersTheRanges()
    {
        _metadata.GetBooty().Returns([new Booty { Name = "3-4", Value = "Gear" }, new Booty { Name = "1-2", Value = "Nothing" }, new Booty { Name = "x", Value = "Bad" }]);

        Assert.Equal([1, 3], Service.GetRecruitTable(RecruitRollTable.Booty).Select(range => range.Min));
    }

    [Fact]
    public void GetRecruitTable_Metachemistry_ParsesTheRanges()
    {
        _metadata.GetMetachemistry().Returns([new Metachemistry { Name = "1-20", Value = "Super-Jump" }]);

        Assert.Equal("Super-Jump", Assert.Single(Service.GetRecruitTable(RecruitRollTable.Metachemistry)).Result);
    }

    [Fact]
    public void GetWeaponNames_SameNameTwice_ListsItOnce()
    {
        _metadata.GetAllWeapons().Returns([new Weapon { Name = "Combi Rifle" }, new Weapon { Name = "combi rifle" }, new Weapon { Name = "Pistol" }]);

        Assert.Equal(["Combi Rifle", "Pistol"], Service.GetWeaponNames());
    }

    [Fact]
    public void GetSkillNames_Stored_ListsThem()
    {
        _metadata.GetSkills().Returns([new Skill { Name = "Camouflage" }]);

        Assert.Equal(["Camouflage"], Service.GetSkillNames());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FormatMovement_Setting_UsesItsUnits(bool inches)
    {
        _settings.GetShowUnitsInInches().Returns(inches);

        Assert.Equal(Movement.Format(new Movement(10, 5), inches), Service.FormatMovement(new Movement(10, 5)));
    }

    [Fact]
    public void FormatMovement_NoSettings_UsesCentimetres() =>
        Assert.Equal(Movement.Format(new Movement(10, 5), inInches: false), WithoutProviders.FormatMovement(new Movement(10, 5)));
}
