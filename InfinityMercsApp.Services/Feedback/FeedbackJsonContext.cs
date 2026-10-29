using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Services.Feedback;

/// <summary>Source-generated JSON contracts for the feedback form, with the web defaults it was sent with.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(FeedbackApiRequest))]
[JsonSerializable(typeof(FeedbackApiResponse))]
internal sealed partial class FeedbackJsonContext : JsonSerializerContext;
