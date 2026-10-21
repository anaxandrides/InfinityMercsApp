namespace InfinityMercsApp.Domain.Validation;

/// <summary>The ids companies and seasons are kept under, here and online.</summary>
public static class DocumentIdentifiers
{
    public const int MaxLength = 64;

    /// <summary>Whether <paramref name="identifier"/> can name a file: 1 to 64 letters, digits, '-' or '_', as a GUID is.</summary>
    public static bool IsValid(string? identifier) =>
        identifier is { Length: > 0 and <= MaxLength } && identifier.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
