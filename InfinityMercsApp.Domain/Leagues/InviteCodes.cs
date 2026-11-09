using System.Text;

namespace InfinityMercsApp.Domain.Leagues;

/// <summary>
/// The codes that let people join a league: six characters, easy to read out, from an alphabet without the look-alike
/// 0/O, 1/I/L. Typed codes are read in any case, with spaces and dashes ignored.
/// </summary>
public static class InviteCodes
{
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    public const int Length = 6;

    /// <summary>Whether <paramref name="code"/> is a code as issued.</summary>
    public static bool IsValid(string? code) => code is { Length: Length } && code.All(Alphabet.Contains);

    /// <summary>The code someone typed, as issued: upper case, without spaces or dashes; null when it can't be one.</summary>
    public static string? Parse(string? typed)
    {
        if (typed is null)
        {
            return null;
        }

        var code = new StringBuilder(Length);
        foreach (var c in typed)
        {
            if (char.IsWhiteSpace(c) || c == '-')
            {
                continue;
            }

            code.Append(char.ToUpperInvariant(c));
        }

        var text = code.ToString();
        return IsValid(text) ? text : null;
    }
}
