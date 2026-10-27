using Bunit;
using InfinityMercsApp.Web.Layout;
using InfinityMercsApp.Web.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityMercsApp.Web.UnitTests.Components.Layout;

/// <summary>The dialog host shows the shared view models' dialogs over the page and hands back the answer.</summary>
public sealed class DialogHostTests : BunitContext
{
    private readonly WebDialogService _dialogs = new();

    public DialogHostTests()
    {
        Services.AddSingleton(_dialogs);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Render_NothingAsked_ShowsNothing() => Assert.Empty(Render<DialogHost>().FindAll(".dialog"));

    [Fact]
    public async Task Confirm_ButtonClicked_AnswersWithIt()
    {
        var host = Render<DialogHost>();

        var answer = _dialogs.ConfirmAsync("Delete?", "Gone for good.", "Delete", "Keep");
        host.WaitForElement(".dialog");
        Assert.Equal("Delete?", host.Find("h2").TextContent);
        Assert.Equal("Gone for good.", host.Find(".message").TextContent);
        Assert.Equal("secondary", host.FindAll("button")[0].ClassName);
        Assert.Equal("primary", host.FindAll("button")[1].ClassName);
        host.FindAll("button")[1].Click();

        Assert.True(await answer);
        Assert.Empty(host.FindAll(".dialog"));
    }

    [Fact]
    public async Task Confirm_BackdropClicked_Cancels()
    {
        var host = Render<DialogHost>();

        var answer = _dialogs.ConfirmAsync("Delete?", "Gone for good.");
        host.WaitForElement(".backdrop").Click();

        Assert.False(await answer);
    }

    [Fact]
    public async Task AskText_Typed_AnswersWithTheText()
    {
        var host = Render<DialogHost>();

        var answer = _dialogs.AskTextAsync("Name", "Name the trooper.", "Captain", 30);
        var input = host.WaitForElement("input");
        Assert.Equal("Captain", input.GetAttribute("value"));
        Assert.Equal("text", input.GetAttribute("inputmode"));
        input.Input("Kestrel");
        host.Find(".dialog").KeyDown("Enter");

        Assert.Equal("Kestrel", await answer);
    }

    [Fact]
    public async Task AskNumber_Escape_Cancels()
    {
        var host = Render<DialogHost>();

        var answer = _dialogs.AskNumberAsync("Roll", "Enter your roll.", 1, 20);
        Assert.Equal("numeric", host.WaitForElement("input").GetAttribute("inputmode"));
        host.Find(".dialog").KeyDown("Escape");

        Assert.Null(await answer);
    }

    [Fact]
    public async Task Alert_OtherKeys_ChangeNothing()
    {
        var host = Render<DialogHost>();

        var answer = _dialogs.ShowAlertAsync("Saved", string.Empty);
        host.WaitForElement(".dialog").KeyDown("Enter");
        host.Find(".dialog").KeyDown("a");

        Assert.False(answer.IsCompleted);
        Assert.Empty(host.FindAll(".message"));
        host.Find("button").Click();
        await answer;
    }

    [Fact]
    public async Task Choose_List_ShowsTheChoicesAsAList()
    {
        var host = Render<DialogHost>();

        var answer = _dialogs.ChooseAsync("Rent", "Cancel", ["Spitfire", "HMG"]);
        host.WaitForElement(".choices");
        host.FindAll("button")[1].Click();

        Assert.Equal("HMG", await answer);
    }
}
