using System.Linq.Expressions;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Repositories;
using NSubstitute;
using DbResume = InfinityMercsApp.Infrastructure.Models.Database.Army.Resume;

namespace InfinityMercsApp.UnitTests.Storage;

/// <summary>The units a Mercs company may recruit: no characters, TAGs, vehicles or Mercs-only duplicates.</summary>
public sealed class FactionProviderMercsOnlyTests
{
    private readonly ISQLiteRepository _repository = Substitute.For<ISQLiteRepository>();
    private readonly FactionProvider _provider;

    // The faction's units as stored; the provider's query filter is applied to them.
    private readonly List<DbResume> _stored =
    [
        Stored(1, "AQUILA GUARD", type: 3, category: 6),
        Stored(2, "JOAN OF ARC", type: 3, category: 10),
        Stored(3, "SQUALO", type: 4, category: 6),
        Stored(4, "SIERRA DRONBOT", type: 8, category: 6),
        Stored(5, "FUSILIER", slug: "merc-fusilier"),
        Stored(6, "BULLETEER", type: null, category: null),
        new DbResume { ResumeKey = "other", FactionId = 102, UnitId = 7, Name = "OTHER FACTION" }
    ];

    public FactionProviderMercsOnlyTests()
    {
        _repository.GetAll(Arg.Any<Expression<Func<DbResume, bool>>>(), Arg.Any<Expression<Func<DbResume, object>>?>())
            .Returns(call => _stored.Where(call.Arg<Expression<Func<DbResume, bool>>>().Compile()).ToList());
        _provider = new FactionProvider(_repository, Substitute.For<IArmyContentStore>());
    }

    private static DbResume Stored(int unitId, string name, int? type = 1, int? category = 1, string? slug = null) =>
        new() { ResumeKey = $"101-{unitId}", FactionId = 101, UnitId = unitId, Name = name, Type = type, Category = category, Slug = slug };

    [Fact]
    public void LeavesOutCharactersTagsVehiclesAndMercsOnlyEntries()
    {
        var names = _provider.GetResumeByFactionMercsOnly(101).Select(unit => unit.Name);

        Assert.Equal(["AQUILA GUARD", "BULLETEER"], names);
    }
}
