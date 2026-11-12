using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Infrastructure.API.Leagues;

/// <summary>Source-generated JSON contracts for what the leagues API sends and takes.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(List<LeagueSummary>))]
[JsonSerializable(typeof(LeagueView))]
[JsonSerializable(typeof(LeagueInvitePreview))]
[JsonSerializable(typeof(LeagueEntryFiles))]
[JsonSerializable(typeof(NewLeague))]
[JsonSerializable(typeof(JoinLeague))]
[JsonSerializable(typeof(LeagueEntryUpdate))]
[JsonSerializable(typeof(NewLeagueGame))]
[JsonSerializable(typeof(LeagueArmyUpdate))]
[JsonSerializable(typeof(List<string>))]
internal sealed partial class LeaguesJsonContext : JsonSerializerContext;
