using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Validation.Company;

namespace InfinityMercsApp.UnitTests.Validation;

public sealed class SavedCompanyFileValidatorTests
{
    private static SavedCompanyEntry Trooper(int index, bool captain = false) => new()
    {
        EntryIndex = index,
        Name = $"Trooper {index}",
        SourceFactionId = 101,
        SourceUnitId = 1,
        IsLieutenant = captain,
        CarriesCaptainUpgrades = captain
    };

    private static SavedCompanyEntry Peripheral(int index, int? parent, bool fromAugment = false) => new()
    {
        EntryIndex = index,
        Name = $"Peripheral {index}",
        SourceFactionId = 101,
        SourceUnitId = 2,
        IsPeripheralUnit = true,
        ParentEntryIndex = parent,
        FromControllerAugment = fromAugment
    };

    private static SavedCompanyEntry Aspect(int index, int of) => new()
    {
        EntryIndex = index,
        Name = $"Aspect {index}",
        SourceFactionId = 101,
        SourceUnitId = 1,
        AspectOf = of,
        CarriesCaptainUpgrades = true
    };

    private static SavedCompanyFile Company(string type = CompanyTypes.StandardVanilla, params IEnumerable<SavedCompanyEntry> entries) => new()
    {
        CompanyName = "Blue Lions",
        CompanyType = type,
        CompanyIdentifier = "b1",
        SourceFactions = [new SavedCompanyFaction { FactionId = 101 }],
        CoreFireteam = type == CompanyTypes.Cohesive ? "Fusilier Core" : string.Empty,
        Entries = entries.ToList()
    };

    private static IReadOnlyList<string> Problems(SavedCompanyFile company) =>
        SavedCompanyFileValidator.Instance.Validate(company).Errors.Select(error => error.ErrorMessage).ToList();

    [Fact]
    public void Validate_CompanyWithAConnectedPeripheral_HasNoProblems() =>
        Assert.Empty(Problems(Company(entries: [Trooper(0, captain: true), Trooper(1), Peripheral(2, parent: 1)])));

    [Fact]
    public void Validate_UnknownType_SaysSo() =>
        Assert.Equal(["\"Lone Wolf Company\" isn't a company type."], Problems(Company("Lone Wolf Company", Trooper(0, captain: true))));

    [Fact]
    public void Validate_NoNameAndABadIdentifier_NamesEach()
    {
        var company = Company(entries: [Trooper(0, captain: true)]);

        var problems = Problems(new SavedCompanyFile
        {
            CompanyName = " ",
            CompanyType = company.CompanyType,
            CompanyIdentifier = "../other",
            SourceFactions = company.SourceFactions,
            Entries = company.Entries
        });

        Assert.Equal(["The company has no name.", "The company's identifier must be 1 to 64 letters, digits, '-' or '_'."], problems);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Validate_NotOneCaptain_SaysSo(int captains) =>
        Assert.Contains("The company must have exactly one Captain.", Problems(Company(entries: Enumerable.Range(0, 3).Select(index => Trooper(index, captain: index < captains)))));

    [Fact]
    public void Validate_PeripheralOfAMissingTrooper_NamesIt() =>
        Assert.Equal(["Peripheral 2 is connected to a trooper the company doesn't have."], Problems(Company(entries: [Trooper(0, captain: true), Peripheral(2, parent: 7)])));

    [Fact]
    public void Validate_PeripheralWithNoTrooper_IsAllowedOnlyFromTheControllerAugment()
    {
        Assert.Empty(Problems(Company(entries: [Trooper(0, captain: true), Peripheral(1, parent: null, fromAugment: true)])));
        Assert.Equal(["Peripheral 1 is a peripheral connected to no trooper."], Problems(Company(entries: [Trooper(0, captain: true), Peripheral(1, parent: null)])));
    }

    [Fact]
    public void Validate_TrooperWithNoUnit_NamesIt()
    {
        var trooper = Trooper(1);

        var problems = Problems(Company(entries: [Trooper(0, captain: true), new SavedCompanyEntry { EntryIndex = 1, Name = trooper.Name, SourceFactionId = 101 }]));

        Assert.Equal(["Trooper 1 names no unit."], problems);
    }

    [Fact]
    public void Validate_CohesiveWithoutItsCoreFireteam_SaysSo()
    {
        var company = Company(CompanyTypes.Cohesive, Trooper(0, captain: true));

        var problems = Problems(new SavedCompanyFile
        {
            CompanyName = company.CompanyName,
            CompanyType = company.CompanyType,
            CompanyIdentifier = company.CompanyIdentifier,
            SourceFactions = company.SourceFactions,
            Entries = company.Entries
        });

        Assert.Equal(["A Cohesive Company needs its core fireteam."], problems);
    }

    [Fact]
    public void Validate_ProxyPackWithAspects_HasNoProblems()
    {
        var wolf = Trooper(0, captain: true);
        var loneWolf = new SavedCompanyEntry
        {
            EntryIndex = wolf.EntryIndex, Name = "Kestrel", SourceFactionId = 101, SourceUnitId = 1,
            IsLieutenant = true, IsLoneWolf = true, CarriesCaptainUpgrades = true
        };

        Assert.Empty(Problems(Company(ProxyPack.CompanyType, loneWolf, Aspect(1, of: 0), Aspect(2, of: 0), Aspect(3, of: 0))));
    }

    [Fact]
    public void Validate_ProxyPackWithAnotherTrooper_SaysSo()
    {
        var loneWolf = new SavedCompanyEntry
        {
            EntryIndex = 0, Name = "Kestrel", SourceFactionId = 101, SourceUnitId = 1, IsLieutenant = true, IsLoneWolf = true
        };

        Assert.Equal(["A Proxy Pack has only its Lone Wolf, its Aspects and their peripherals."], Problems(Company(ProxyPack.CompanyType, loneWolf, Trooper(1))));
    }

    [Fact]
    public void Validate_AspectOutsideAProxyPack_SaysSo() =>
        Assert.Contains("Only a Proxy Pack has a Lone Wolf and Aspects.", Problems(Company(entries: [Trooper(0, captain: true), Aspect(1, of: 0)])));
}
