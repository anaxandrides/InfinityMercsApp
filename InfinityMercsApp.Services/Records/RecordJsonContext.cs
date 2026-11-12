using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.Services.Records;

/// <summary>
/// Source-generated JSON contracts for company and season files, used under each of <see cref="RecordJson"/>'s options
/// (and the catalogs' own), so reading and writing them needs no reflection.
/// </summary>
[JsonSerializable(typeof(SavedCompanyFile))]
[JsonSerializable(typeof(SavedCompanyEntry))]
[JsonSerializable(typeof(SeasonFile))]
[JsonSerializable(typeof(SeasonFileHeader))]
[JsonSerializable(typeof(CompanyFileHeader))]
internal sealed partial class RecordJsonContext : JsonSerializerContext;
