namespace InfinityMercsApp.Services;

public sealed class FeedbackApiRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("_replyto")]
    public string ReplyTo { get; init; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("_captcha")]
    public string Captcha { get; init; } = "false";

    [System.Text.Json.Serialization.JsonPropertyName("_template")]
    public string Template { get; init; } = "table";
}
