using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>
/// What has happened to one trooper this season beyond its experience (which is added up from the rounds):
/// the injuries it carries, the perks it has chosen, and what downtime and services gave or took away.
/// </summary>
public class SeasonTrooper
{
    public List<Injury> Injuries { get; set; } = [];

    /// <summary>Perk node ids, in the order they were chosen.</summary>
    public List<string> Perks { get; set; } = [];

    public List<string> SkillsGained { get; set; } = [];

    public List<string> CharacteristicsGained { get; set; } = [];

    public List<string> CharacteristicsLost { get; set; } = [];
}
