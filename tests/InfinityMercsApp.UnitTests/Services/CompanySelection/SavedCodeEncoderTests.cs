using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection;

public sealed class SavedCodeEncoderTests
{
    private readonly SavedCodeEncoder _encoder = new(Army101Data.Create());

    private CodedNames Encode(params IEnumerable<string> names) =>
        _encoder.Encode(names, ProfileEntryKind.Weapons, [0, Army101Data.PanOceania]);

    [Fact]
    public void CodesKnownNamesIgnoringCaseAndSpacing()
    {
        var coded = Encode("Combi Rifle", " combi   RIFLE ");

        Assert.Equal([33], coded.Codes.Select(c => c.Id));
        Assert.Null(coded.Codes[0].Extra);
        Assert.Empty(coded.CustomNames);
    }

    [Fact]
    public void CodesBracketedExtrasInIdOrder()
    {
        var code = Assert.Single(Encode("Combi Rifle (+1, -3)").Codes);

        Assert.Equal(33, code.Id);
        Assert.Equal([6, 298], code.Extra!);
    }

    [Fact]
    public void KeepsUncodedNamesAsCustomText()
    {
        var coded = Encode("Laser Spoon", "Combi Rifle (+1, Glitter)");

        Assert.Equal(["Laser Spoon", "Combi Rifle (+1, Glitter)"], coded.CustomNames);
        Assert.Equal([298], Assert.Single(coded.Codes).Extra!);
    }

    [Fact]
    public void RoundTripsThroughSavedCodeNames()
    {
        var names = new List<string> { "Combi Rifle (+1)", "AP CC Weapon" };

        var coded = Encode(names);

        Assert.Equal(names, SavedCodeNames.Resolve(Army101Data.Definition.Filters, coded.Codes, ProfileEntryKind.Weapons));
    }
}
