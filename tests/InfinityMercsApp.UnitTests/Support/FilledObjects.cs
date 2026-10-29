using System.Collections;
using System.Reflection;

namespace InfinityMercsApp.UnitTests.Support;

/// <summary>
/// An object with every settable property given a value (strings, numbers, flags, enums, one item in each list and
/// dictionary, nested objects a few levels deep), so a serializer's handling of every member shows in its output.
/// </summary>
internal static class FilledObjects
{
    private const int MaxDepth = 4;

    public static T Of<T>()
        where T : new() => (T)(Fill(typeof(T), 0) ?? throw new InvalidOperationException($"{typeof(T).Name} couldn't be filled."));

    private static object? Fill(Type type, int depth)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (ValueFor(underlying) is { } value)
        {
            return value;
        }

        if (depth >= MaxDepth)
        {
            return null;
        }

        if (underlying.IsArray)
        {
            var array = Array.CreateInstance(underlying.GetElementType() ?? typeof(object), 1);
            array.SetValue(Fill(underlying.GetElementType() ?? typeof(object), depth + 1), 0);
            return array;
        }

        if (DictionaryTypes(underlying) is [var keyType, var valueType])
        {
            var dictionary = (IDictionary?)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));
            dictionary?.Add(Fill(keyType, depth + 1) ?? "key", Fill(valueType, depth + 1));
            return dictionary;
        }

        if (ElementType(underlying) is { } elementType)
        {
            var list = (IList?)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            list?.Add(Fill(elementType, depth + 1));
            return list;
        }

        if (underlying.IsAbstract || underlying.IsInterface || underlying.GetConstructor(Type.EmptyTypes) is null)
        {
            return null;
        }

        var instance = Activator.CreateInstance(underlying);
        foreach (var property in underlying.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.SetMethod is { IsPublic: true } && property.GetIndexParameters().Length == 0 &&
                Fill(property.PropertyType, depth + 1) is { } propertyValue)
            {
                property.SetValue(instance, propertyValue);
            }
        }

        return instance;
    }

    private static object? ValueFor(Type type) => type switch
    {
        _ when type == typeof(string) => "text",
        _ when type == typeof(int) => 3,
        _ when type == typeof(long) => 4L,
        _ when type == typeof(double) => 1.5,
        _ when type == typeof(decimal) => 2.5m,
        _ when type == typeof(bool) => true,
        _ when type == typeof(DateTimeOffset) => new DateTimeOffset(2026, 10, 29, 9, 30, 0, TimeSpan.Zero),
        _ when type == typeof(DateTime) => new DateTime(2026, 10, 29, 9, 30, 0, DateTimeKind.Utc),
        _ when type == typeof(Guid) => new Guid("6f9619ff-8b86-d011-b42d-00c04fc964ff"),
        _ when type.IsEnum => Enum.GetValues(type).Cast<object>().Last(),
        _ => null
    };

    private static IReadOnlyList<Type>? DictionaryTypes(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() is var definition &&
        (definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
            ? type.GetGenericArguments()
            : null;

    private static Type? ElementType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() is var definition &&
        (definition == typeof(List<>) || definition == typeof(IList<>) || definition == typeof(IReadOnlyList<>) ||
         definition == typeof(ICollection<>) || definition == typeof(IReadOnlyCollection<>) || definition == typeof(IEnumerable<>))
            ? type.GetGenericArguments()[0]
            : null;
}
