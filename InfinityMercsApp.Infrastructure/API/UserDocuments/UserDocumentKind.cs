using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.API.UserDocuments;

/// <summary>The kinds of file a user keeps online, which tell them apart in storage.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserDocumentKind>))]
public enum UserDocumentKind
{
    Company,
    Season
}
