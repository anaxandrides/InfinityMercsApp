using System.Text.Json.Serialization.Metadata;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json;

/// <summary>A source-generated context and the types its contracts start from.</summary>
internal sealed record ContractRoots(IJsonTypeInfoResolver Resolver, IReadOnlyList<Type> Types);
