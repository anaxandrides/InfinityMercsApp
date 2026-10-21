using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.UnitTests.Captain;

public sealed class ProfileTextTests
{
    private static readonly string NewLine = Environment.NewLine;

    [Fact]
    public void Lines_BlanksAndPlaceholders_DropsThem() =>
        Assert.Equal<string>(["Combi Rifle", "Pistol"], ProfileText.Lines($" Combi Rifle {NewLine}-{NewLine}{NewLine}Pistol"));

    [Fact]
    public void AppendDistinct_ItemAlreadyListed_SkipsIt() =>
        Assert.Equal($"Combi Rifle{NewLine}Pistol{NewLine}Sniper", ProfileText.AppendDistinct($"Combi Rifle{NewLine}Pistol", ["pistol", "Sniper"]));

    [Fact]
    public void AppendDistinct_NothingAdded_NormalisesOrKeepsText()
    {
        Assert.Equal("-", ProfileText.AppendDistinct("  -  ", []));
        Assert.Equal("  -  ", ProfileText.AppendDistinct("  -  ", [], keepTextWhenNothingAdded: true));
        Assert.Equal("-", ProfileText.AppendDistinct(null, [], keepTextWhenNothingAdded: true));
    }

    [Fact]
    public void AppendMarked_Additions_MarksEach() =>
        Assert.Equal($"Pistol{NewLine}+ Sniper", ProfileText.AppendMarked("Pistol", ["Sniper"]));

    [Fact]
    public void AppendChoices_BlanksDashesAndDuplicates_TrimsAndSkipsThem() =>
        Assert.Equal<string>(["Pistol", "Sniper"], ProfileText.AppendChoices([" Pistol ", ""], "Sniper", "-", null, "pistol"));
}
