using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.Season;

/// <summary>The second profiles of PanOceania's Transmutation (X) troopers, from the reference army.</summary>
public sealed class TransmutedFormsTests
{
    private readonly TransmutedForms _forms = new(Army101Data.Create());

    private static SavedCompanyEntry Entry(string unitName) => new()
    {
        EntryIndex = 1,
        SourceFactionId = Army101Data.PanOceania,
        SourceUnitId = Army101Data.UnitNamed(unitName).Id,
        ProfileKey = $"{unitName}|option|10|-|lt:0"
    };

    [Fact]
    public void For_Diggers_IsBattleRavagedAfterOneWound()
    {
        var form = _forms.For(Entry("DIGGERS, Armed Prospectors"), Army101Data.PanOceania, armorName: null, vitality: 2, showUnitsInInches: false);

        Assert.NotNull(form);
        Assert.Equal(("BATTLE-RAVAGED", 1), (form.Name, form.Threshold));
        Assert.Contains(new StatValue(Stat.Arm, "2"), form.Statline);
        Assert.Contains(new StatValue(Stat.Vita, "2"), form.Statline);
    }

    [Fact]
    public void For_VitaGained_RaisesTheThreshold()
    {
        var form = _forms.For(Entry("DIGGERS, Armed Prospectors"), Army101Data.PanOceania, armorName: null, vitality: 3, showUnitsInInches: false);

        Assert.Equal(2, form?.Threshold);
        Assert.Contains(new StatValue(Stat.Vita, "3"), form?.Statline ?? []);
    }

    [Fact]
    public void For_SymbioResonance_NeverDegrades() =>
        Assert.Null(_forms.For(Entry("DIGGERS, Armed Prospectors"), Army101Data.PanOceania, "Symbio - Resonance", 2, showUnitsInInches: false));

    [Fact]
    public void For_NoTransmutationX_HasNoForm() =>
        Assert.Null(_forms.For(Entry("FUSILIERS"), Army101Data.PanOceania, armorName: null, vitality: 1, showUnitsInInches: false));
}
