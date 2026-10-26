using Bunit;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Web.Components.Base;
using InfinityMercsApp.Web.Platform;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Components.Base;

/// <summary>A logo shows the first of its candidates that loads, and a blank of the same size when none does.</summary>
public sealed class LogoTests : BunitContext
{
    public LogoTests()
    {
        var package = Substitute.For<IAppPackageFiles>();
        package.ExistsAsync("SVGCache/units/fusilier.svg").Returns(true);
        Services.AddSingleton(new LogoUrls(new RemoteLogoStore(), package));
    }

    [Fact]
    public void Render_Candidates_ShowsTheFirstThenTheNextWhenItFails()
    {
        var logo = Render<Logo>(parameters => parameters
            .Add(p => p.Candidates, ["https://cdn.example/1.svg", "SVGCache/units/fusilier.svg"])
            .Add(p => p.Size, 24)
            .Add(p => p.Class, "unit"));

        Assert.Equal("https://cdn.example/1.svg", logo.Find("img").GetAttribute("src"));
        Assert.Equal("24", logo.Find("img").GetAttribute("width"));
        logo.Find("img").TriggerEvent("onerror", EventArgs.Empty);
        Assert.Equal("package/SVGCache/units/fusilier.svg", logo.Find("img").GetAttribute("src"));
        logo.Find("img").TriggerEvent("onerror", EventArgs.Empty);
        Assert.Equal("width:24px;height:24px", logo.Find(".logo-blank").GetAttribute("style"));
    }

    [Fact]
    public void Render_OneSource_ShowsIt() =>
        Assert.Equal("https://cdn.example/1.svg", Render<Logo>(parameters => parameters.Add(p => p.Source, "https://cdn.example/1.svg")).Find("img").GetAttribute("src"));

    [Fact]
    public void Render_Nothing_IsBlank() => Assert.Single(Render<Logo>().FindAll(".logo-blank"));

    [Fact]
    public void Render_SameCandidatesAgain_KeepsItsPlace()
    {
        IReadOnlyList<string> candidates = ["https://cdn.example/1.svg", "https://cdn.example/2.svg"];
        var logo = Render<Logo>(parameters => parameters.Add(p => p.Candidates, candidates));
        logo.Find("img").TriggerEvent("onerror", EventArgs.Empty);

        logo.Render(parameters => parameters.Add(p => p.Candidates, candidates.ToList()));
        Assert.Equal("https://cdn.example/2.svg", logo.Find("img").GetAttribute("src"));

        logo.Render(parameters => parameters.Add(p => p.Candidates, ["https://cdn.example/3.svg"]));
        Assert.Equal("https://cdn.example/3.svg", logo.Find("img").GetAttribute("src"));
    }
}
