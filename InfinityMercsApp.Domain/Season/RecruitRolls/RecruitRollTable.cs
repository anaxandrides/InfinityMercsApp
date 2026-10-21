using System.Text.Json.Serialization;

namespace InfinityMercsApp.Domain.Season.RecruitRolls;

/// <summary>The d20 tables a trooper with the matching skill rolls on once, when it is first bought.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecruitRollTable>))]
public enum RecruitRollTable
{
    Booty,
    Metachemistry
}
