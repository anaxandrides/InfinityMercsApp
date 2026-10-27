using Bunit;
using InfinityMercsApp.Presentation.Text;
using InfinityMercsApp.Services.Links;
using InfinityMercsApp.Web.Components.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Graphics;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Components.Base;

/// <summary>Rich text: each run in its colour, links opening in a new tab, and the gaps between entries.</summary>
public sealed class RichTextViewTests : BunitContext
{
    public RichTextViewTests() => Services.AddSingleton(Substitute.For<ILinkLauncher>());

    [Fact]
    public void Render_Runs_ShowsEachAsItIs()
    {
        var text = new RichText(
        [
            new RichTextRun("Camouflage", Link: "https://wiki.example/camo"),
            RichTextRun.EntryGap,
            new RichTextRun("Red", new Color(1f, 0f, 0f)),
            new RichTextRun("Plain")
        ]);

        var view = Render<RichTextView>(parameters => parameters.Add(p => p.Text, text).Add(p => p.Class, "skills"));

        var link = view.Find("a");
        Assert.Equal("https://wiki.example/camo", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Single(view.FindAll(".entry-gap"));
        Assert.Equal("color:#FF0000FF", view.FindAll("span > span")[1].GetAttribute("style"));
        Assert.Null(view.FindAll("span > span")[2].GetAttribute("style"));
        Assert.Contains("rich skills", view.Find("span").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_NoText_IsEmpty() => Assert.Empty(Render<RichTextView>().FindAll("span > span"));
}
