using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.Services.Stores;

/// <summary>Source-generated JSON contract for the bundled market files.</summary>
[JsonSerializable(typeof(Store))]
internal sealed partial class StoreJsonContext : JsonSerializerContext;
