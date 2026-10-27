using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>The shared view models' dialogs, shown one at a time in the site's dialog host.</summary>
public sealed class WebDialogServiceTests
{
    private static WebDialogService Over(AnsweringPresenter presenter) => new() { Presenter = presenter };

    [Fact]
    public async Task ShowAlertAsync_NoHostYet_ReturnsAtOnce() => await new WebDialogService().ShowAlertAsync("Title", "Message");

    [Fact]
    public async Task ShowAlertAsync_Message_ShowsItWithOneButton()
    {
        var presenter = new AnsweringPresenter(new DialogAnswer("Got it", string.Empty));

        await Over(presenter).ShowAlertAsync("Saved", "The company is saved.", "Got it");

        var shown = Assert.Single(presenter.Shown);
        Assert.Equal("Saved", shown.Title);
        Assert.Equal(["Got it"], shown.Buttons);
        Assert.Equal("Got it", shown.CancelButton);
    }

    [Theory]
    [InlineData("Delete", true)]
    [InlineData("Keep", false)]
    [InlineData(null, false)]
    public async Task ConfirmAsync_Answer_IsTrueOnlyForAccept(string? button, bool expected)
    {
        var presenter = new AnsweringPresenter(new DialogAnswer(button, string.Empty));

        Assert.Equal(expected, await Over(presenter).ConfirmAsync("Delete?", "Gone for good.", "Delete", "Keep"));
        Assert.Equal(["Keep", "Delete"], presenter.Shown.Single().Buttons);
    }

    [Theory]
    [InlineData("OK", "7", 7)]
    [InlineData("OK", "0", null)]
    [InlineData("OK", "11", null)]
    [InlineData("OK", "seven", null)]
    [InlineData("Cancel", "7", null)]
    public async Task AskNumberAsync_Answer_IsTheNumberWhenInRange(string button, string text, int? expected)
    {
        var presenter = new AnsweringPresenter(new DialogAnswer(button, text));

        Assert.Equal(expected, await Over(presenter).AskNumberAsync("Roll", "Enter your roll.", 1, 10));
        var shown = presenter.Shown.Single();
        Assert.Equal(DialogKind.Number, shown.Kind);
        Assert.Equal(2, shown.MaxLength);
    }

    [Fact]
    public async Task AskTextAsync_Ok_IsTheTrimmedText()
    {
        var presenter = new AnsweringPresenter(new DialogAnswer("OK", "  Kestrel  "));

        Assert.Equal("Kestrel", await Over(presenter).AskTextAsync("Name", "Name the trooper.", "Captain", 30));
        var shown = presenter.Shown.Single();
        Assert.Equal(DialogKind.Text, shown.Kind);
        Assert.Equal("Captain", shown.InitialText);
        Assert.Equal(30, shown.MaxLength);
    }

    [Fact]
    public async Task AskTextAsync_Cancelled_IsNull() =>
        Assert.Null(await Over(new AnsweringPresenter(DialogAnswer.Dismissed)).AskTextAsync("Name", "Name the trooper.", "Captain", 30));

    [Fact]
    public async Task ChooseAsync_Option_IsThatOption()
    {
        var presenter = new AnsweringPresenter(new DialogAnswer("Spitfire", string.Empty));

        Assert.Equal("Spitfire", await Over(presenter).ChooseAsync("Rent", "Cancel", ["Spitfire", "HMG"]));
        var shown = presenter.Shown.Single();
        Assert.True(shown.IsList);
        Assert.Equal(["Spitfire", "HMG", "Cancel"], shown.Buttons);
    }

    [Fact]
    public async Task ChooseAsync_Cancelled_IsNull() =>
        Assert.Null(await Over(new AnsweringPresenter(new DialogAnswer("Cancel", string.Empty))).ChooseAsync("Rent", "Cancel", ["Spitfire"]));

    [Fact]
    public async Task ShowAlertAsync_SecondWhileFirstShows_WaitsItsTurn()
    {
        var presenter = new AnsweringPresenter();
        var dialogs = Over(presenter);

        var first = dialogs.ShowAlertAsync("First", "One");
        var second = dialogs.ShowAlertAsync("Second", "Two");

        Assert.Equal(["First"], presenter.Shown.Select(request => request.Title));
        presenter.Shown[0].Complete(DialogAnswer.Dismissed);
        await first;
        await WaitForShown(presenter, 2);
        presenter.Shown[1].Complete(DialogAnswer.Dismissed);
        await second;
        Assert.Equal(["First", "Second"], presenter.Shown.Select(request => request.Title));
    }

    [Fact]
    public async Task Complete_Twice_KeepsTheFirstAnswer()
    {
        var request = new DialogRequest { Title = "Title", Buttons = ["OK"] };

        request.Complete(new DialogAnswer("OK", string.Empty));
        request.Complete(DialogAnswer.Dismissed);

        Assert.Equal("OK", (await request.Answer).Button);
    }

    private static async Task WaitForShown(AnsweringPresenter presenter, int count)
    {
        for (var tries = 0; tries < 100 && presenter.Shown.Count < count; tries++)
        {
            await Task.Delay(10);
        }

        Assert.Equal(count, presenter.Shown.Count);
    }
}
