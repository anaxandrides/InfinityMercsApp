using System.Text.Json.Serialization;

namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>How a game went for one side.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<GameResult>))]
public enum GameResult
{
    Win,
    Draw,
    Loss
}
