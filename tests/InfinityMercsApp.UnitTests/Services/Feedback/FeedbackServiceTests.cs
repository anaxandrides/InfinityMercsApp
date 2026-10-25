using System.Net;
using System.Text.Json;
using InfinityMercsApp.Services.Feedback;

namespace InfinityMercsApp.UnitTests.Services.Feedback;

public sealed class FeedbackServiceTests
{
    private static readonly DeviceDetails Device = new("1.2.3", "Android", "Pixel 9", "Kestrel's phone");

    private static readonly FeedbackSubmission Submission = new()
    {
        Category = " Bug ",
        Title = " Crash on save ",
        Description = " It crashed. ",
        UserEmail = " kestrel@example.com "
    };

    private static Task<FeedbackSubmitResult> Submit(FormHandler handler) =>
        new FeedbackService(new HttpClient(handler), Device).SubmitAsync(Submission);

    [Fact]
    public async Task SubmitAsync_Accepted_SendsTheTrimmedFormWithTheDevice()
    {
        var handler = new FormHandler(HttpStatusCode.OK, """{"success":"true"}""");

        var result = await Submit(handler);

        Assert.True(result.Success);
        Assert.Equal("Feedback email sent successfully.", result.Message);
        Assert.Equal("formsubmit.co", handler.SentTo?.Host);
        using var form = JsonDocument.Parse(handler.SentBody ?? "{}");
        var fields = form.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("Infinity Mercs App Feedback/Bug - Crash on save", fields["subject"]);
        Assert.Equal("kestrel@example.com", fields["_replyto"]);
        var message = fields["message"] ?? string.Empty;
        Assert.Contains("Category: Bug", message, StringComparison.Ordinal);
        Assert.Contains("App Version: 1.2.3", message, StringComparison.Ordinal);
        Assert.Contains("Device Name: Kestrel's phone", message, StringComparison.Ordinal);
        Assert.EndsWith("It crashed.", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitAsync_AcceptedWithoutJson_StillSucceeds() =>
        Assert.True((await Submit(new FormHandler(HttpStatusCode.OK, "thanks", "text/plain"))).Success);

    [Fact]
    public async Task SubmitAsync_HtmlError_SaysTheEndpointIsWrong()
    {
        var result = await Submit(new FormHandler(HttpStatusCode.NotFound, "<html></html>", "text/html"));

        Assert.False(result.Success);
        Assert.Contains("returned HTML (404", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubmitAsync_ErrorWithABody_ReportsTheBody()
    {
        var result = await Submit(new FormHandler(HttpStatusCode.BadRequest, "Bad address"));

        Assert.False(result.Success);
        Assert.Equal("Email send failed: Bad address", result.Message);
    }

    [Fact]
    public async Task SubmitAsync_ErrorWithoutABody_ReportsTheStatus()
    {
        var result = await Submit(new FormHandler(HttpStatusCode.InternalServerError, string.Empty));

        Assert.Equal("Email send failed: 500 InternalServerError", result.Message);
    }

    [Theory]
    [InlineData(null, "Feedback email sent successfully.")]
    [InlineData("https://issues/1", "Feedback email sent successfully: https://issues/1")]
    public void Ok_IssueUrl_IsInTheMessage(string? issueUrl, string expected)
    {
        var result = FeedbackSubmitResult.Ok(issueUrl);

        Assert.True(result.Success);
        Assert.Equal(expected, result.Message);
        Assert.Equal(issueUrl, result.IssueUrl);
    }
}
