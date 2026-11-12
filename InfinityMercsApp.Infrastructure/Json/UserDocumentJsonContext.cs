using System.Text.Json.Serialization;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>Source-generated JSON contracts for the companies and seasons the account's API keeps (<see cref="UserDocumentTypes"/>).</summary>
[JsonSerializable(typeof(UserDocumentWrite<SavedCompanyFile>))]
[JsonSerializable(typeof(UserDocumentView<SavedCompanyFile>))]
[JsonSerializable(typeof(IReadOnlyList<UserDocumentView<SavedCompanyFile>>))]
[JsonSerializable(typeof(UserDocumentWrite<SeasonFile>))]
[JsonSerializable(typeof(UserDocumentView<SeasonFile>))]
[JsonSerializable(typeof(IReadOnlyList<UserDocumentView<SeasonFile>>))]
public sealed partial class UserDocumentJsonContext : JsonSerializerContext;
