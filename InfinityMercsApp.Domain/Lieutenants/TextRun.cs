namespace InfinityMercsApp.Domain.Lieutenants;

/// <summary>A piece of display text, flagged when it is the word "Lieutenant" so screens can highlight it.</summary>
public sealed record TextRun(string Text, bool IsLieutenant)
{
    public static TextRun Plain(string text) => new(text, false);

    public static TextRun Lieutenant(string text) => new(text, true);
}
