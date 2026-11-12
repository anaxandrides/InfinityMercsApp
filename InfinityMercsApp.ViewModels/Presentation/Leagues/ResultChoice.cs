using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>How a game went for the recording player's company, as the form offers it.</summary>
public sealed record ResultChoice(GameResult Result, string Text)
{
    public static IReadOnlyList<ResultChoice> All { get; } =
    [
        new(GameResult.Win, "Won"),
        new(GameResult.Draw, "Drew"),
        new(GameResult.Loss, "Lost")
    ];

    /// <summary>How it went for the other company.</summary>
    public GameResult Opposite => Result switch
    {
        GameResult.Win => GameResult.Loss,
        GameResult.Loss => GameResult.Win,
        _ => GameResult.Draw
    };

    public override string ToString() => Text;
}
