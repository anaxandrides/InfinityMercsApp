using System.Text.Json.Serialization;

namespace InfinityMercsApp.Domain.Season.Troopers;

/// <summary>The lasting injuries a trooper can take from the injury table.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Injury>))]
public enum Injury
{
    BattleFury,
    PuncturedLung,
    Arms,
    BrainInjury,
    LegsInjury,
    BodyInjury,
    EyesInjury,
    ShellShocked
}
