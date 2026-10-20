using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.API.UserDocuments;

/// <summary>The kinds of file a user keeps online.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserDocumentKind>))]
public enum UserDocumentKind
{
    Company,
    Season
}
