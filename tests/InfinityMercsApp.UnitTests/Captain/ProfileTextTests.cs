using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.UnitTests.Captain;

public sealed class ProfileTextTests
{
    private static readonly string NewLine = Environment.NewLine;

    [Fact]
    public void Lines_DropBlanksAndPlaceholders() =>
        Assert.Equal<string>(["Combi Rifle", "Pistol"], ProfileText.Lines($" Combi Rifle {NewLine}-{NewLine}{NewLine}Pistol"));

    [Fact]
    public void AppendDistinct_SkipsItemsAlreadyListed() =>
        Assert.Equal($"Combi Rifle{NewLine}Pistol{NewLine}Sniper", ProfileText.AppendDistinct($"Combi Rifle{NewLine}Pistol", ["pistol", "Sniper"]));

    [Fact]
    public void AppendDistinct_NormalisesOrKeepsTextWhenNothingIsAdded()
    {
        Assert.Equal("-", ProfileText.AppendDistinct("  -  ", []));
        Assert.Equal("  -  ", ProfileText.AppendDistinct("  -  ", [], keepTextWhenNothingAdded: true));
        Assert.Equal("-", ProfileText.AppendDistinct(null, [], keepTextWhenNothingAdded: true));
    }

    [Fact]
    public void AppendMarked_MarksEachAddition() =>
        Assert.Equal($"Pistol{NewLine}+ Sniper", ProfileText.AppendMarked("Pistol", ["Sniper"]));

    [Fact]
    public void AppendChoices_TrimsAndSkipsBlanksDashesAndDuplicates() =>
        Assert.Equal<string>(["Pistol", "Sniper"], ProfileText.AppendChoices([" Pistol ", ""], "Sniper", "-", null, "pistol"));
}
