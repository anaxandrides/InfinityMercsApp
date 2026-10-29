namespace InfinityMercsApp.Services.Feedback;

public sealed class FeedbackApiRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("_replyto")]
    public string ReplyTo { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("_captcha")]
    public string Captcha { get; set; } = "false";

    [System.Text.Json.Serialization.JsonPropertyName("_template")]
    public string Template { get; set; } = "table";
}
