namespace InfinityMercsApp.Infrastructure.Json;

internal enum JsonScalarKind
{
    /// <summary>The property was not in the payload.</summary>
    Missing = 0,
    Null,
    Number,
    String,
    True,
    False,
    /// <summary>An object or array where a scalar was expected.</summary>
    Other
}
