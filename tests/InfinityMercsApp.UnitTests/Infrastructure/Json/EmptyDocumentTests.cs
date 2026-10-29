using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Json.Dtos;
using InfinityMercsApp.Services.Feedback;
using InfinityMercsApp.Services.Glossary;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.Services.Telemetry;
using ApiMetadataDocument = InfinityMercsApp.Infrastructure.Models.API.Metadata.MetadataDocument;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json;

/// <summary>
/// A member the JSON leaves out keeps its initial value through every source-generated contract, as it did through
/// reflection. The source generator sets init-only properties left out of the JSON to null or zero (dotnet/runtime
/// PR 124650, unreleased), so those must be settable.
/// </summary>
public sealed class EmptyDocumentTests
{
    public static TheoryData<string> Contexts { get; } = ["Records", "Stores", "Sync", "Telemetry", "Glossary", "Feedback", "ArmyDefinition", "Metadata", "ArmyMirror", "UserDocuments", "ArmyPayloads"];

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Contracts_InitOnlyMembers_StartAtTheirDefault(string context)
    {
        var roots = ContextFor(context);
        var options = new JsonSerializerOptions { TypeInfoResolver = roots.Resolver };
        var lost = new List<string>();
        foreach (var info in Reachable(options, roots.Types))
        {
            if (info.Type.IsAbstract || info.Type.GetConstructor(Type.EmptyTypes) is null)
            {
                continue;
            }

            var instance = Activator.CreateInstance(info.Type);
            foreach (var property in info.Properties.Where(property => !property.IsRequired))
            {
                if (property.AttributeProvider is PropertyInfo { SetMethod: { } setter } member && IsInitOnly(setter) &&
                    !Equals(member.GetValue(instance), DefaultOf(member.PropertyType)))
                {
                    lost.Add($"{info.Type.Name}.{member.Name}");
                }
            }
        }

        Assert.True(lost.Count == 0, "Initial values a missing member loses: " + string.Join(", ", lost.Distinct()));
    }

    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(System.Runtime.CompilerServices.IsExternalInit));

    private static object? DefaultOf(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

    private static ContractRoots ContextFor(string name) => name switch
    {
        "Records" => new(RecordJsonContext.Default, [typeof(SavedCompanyFile), typeof(SeasonFile), typeof(SavedCompanyEntry)]),
        "Stores" => new(StoreJsonContext.Default, [typeof(Store)]),
        "Sync" => new(SyncJsonContext.Default, [typeof(SyncState)]),
        "Telemetry" => new(TelemetryJsonContext.Default, [typeof(TelemetryEvent), typeof(TelemetrySettings)]),
        "Glossary" => new(GlossaryJsonContext.Default, [typeof(GlossaryPayload)]),
        "Feedback" => new(FeedbackJsonContext.Default, [typeof(FeedbackApiRequest), typeof(FeedbackApiResponse)]),
        "ArmyDefinition" => new(ArmyDefinitionJsonContext.Default, [typeof(ArmyDefinition)]),
        "Metadata" => new(MetadataJsonContext.Default, [typeof(ApiMetadataDocument)]),
        "ArmyMirror" => new(ArmyMirrorJsonContext.Default, [typeof(ArmyMirrorManifest)]),
        "UserDocuments" => new(UserDocumentJsonContext.Default, [typeof(UserDocumentView<SavedCompanyFile>), typeof(UserDocumentView<SeasonFile>)]),
        _ => new(ArmyJsonContext.Default, [typeof(ArmyPayloadDto)])
    };

    // Every object contract the roots lead to: properties, list items and dictionary values.
    private static IEnumerable<JsonTypeInfo> Reachable(JsonSerializerOptions options, IReadOnlyList<Type> roots)
    {
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>(roots);
        while (queue.TryDequeue(out var type))
        {
            if (!seen.Add(type))
            {
                continue;
            }

            JsonTypeInfo info;
            try
            {
                info = options.GetTypeInfo(type);
            }
            catch (NotSupportedException)
            {
                continue;
            }

            foreach (var next in info.Properties.Select(property => property.PropertyType).Append(info.ElementType).Append(info.KeyType).OfType<Type>())
            {
                queue.Enqueue(Nullable.GetUnderlyingType(next) ?? next);
            }

            if (info.Kind == JsonTypeInfoKind.Object)
            {
                yield return info;
            }
        }
    }
}
