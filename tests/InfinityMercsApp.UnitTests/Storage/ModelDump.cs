using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.UnitTests.Storage;

/// <summary>
/// Writes any domain object graph as deterministic text: every public readable property (computed ones
/// included), lists in order. Two models dump the same exactly when everything callers can read off them
/// is the same, which is what the storage round-trip tests compare.
/// </summary>
internal static class ModelDump
{
    public static string Of(object? value)
    {
        var builder = new StringBuilder();
        Write(builder, value, 0);
        return builder.ToString();
    }

    private static void Write(StringBuilder builder, object? value, int depth)
    {
        if (depth > 40)
        {
            throw new InvalidOperationException("Model graph is too deep; is there a cycle?");
        }

        switch (value)
        {
            case null:
                builder.Append("null");
                return;
            case string text:
                builder.Append('"').Append(text).Append('"');
                return;
            case bool flag:
                builder.Append(flag ? "true" : "false");
                return;
            case Enum:
                builder.Append(value);
                return;
            case IFormattable formattable when value.GetType().IsPrimitive || value is decimal:
                builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                return;
            case ArmyFilterCatalog catalog:
                WriteCatalog(builder, catalog, depth);
                return;
            case ITuple tuple:
                builder.Append('(');
                for (var i = 0; i < tuple.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    Write(builder, tuple[i], depth + 1);
                }

                builder.Append(')');
                return;
            case IEnumerable items:
                builder.Append('[');
                var first = true;
                foreach (var item in items)
                {
                    if (!first)
                    {
                        builder.Append(", ");
                    }

                    first = false;
                    Write(builder, item, depth + 1);
                }

                builder.Append(']');
                return;
        }

        var type = value.GetType();
        builder.Append(type.Name).Append(" {");
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal);
        foreach (var property in properties)
        {
            builder.Append(' ').Append(property.Name).Append('=');
            Write(builder, property.GetValue(value), depth + 1);
            builder.Append(';');
        }

        builder.Append(" }");
    }

    private static void WriteCatalog(StringBuilder builder, ArmyFilterCatalog catalog, int depth)
    {
        builder.Append("ArmyFilterCatalog {");
        foreach (var section in catalog.Sections)
        {
            builder.Append(' ').Append(section).Append('=');
            Write(builder, catalog.Items(section), depth + 1);
            builder.Append(';');
        }

        foreach (var section in catalog.KeyedSections)
        {
            builder.Append(" keyed:").Append(section).Append('=');
            Write(builder, catalog.KeyedItems(section), depth + 1);
            builder.Append(';');
        }

        builder.Append(" }");
    }
}
