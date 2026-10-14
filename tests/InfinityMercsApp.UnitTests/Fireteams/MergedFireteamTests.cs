using InfinityMercsApp.Domain.Fireteams;
using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.UnitTests.Fireteams;

public sealed class MergedFireteamTests
{
    [Fact]
    public void MergeChart_AddsCountsAndWidensSlotLimitsAcrossCharts()
    {
        var first = new FireteamChart
        {
            Teams =
            [
                new FireteamTeam
                {
                    Name = " Kestrel ",
                    TypeList = ["CORE", "HARIS"],
                    Units = [new FireteamUnitLimit { Name = "Orc", Min = 1, Max = 2 }]
                },
                new FireteamTeam { Name = " ", TypeList = ["DUO"] }
            ]
        };
        var second = new FireteamChart
        {
            Teams =
            [
                new FireteamTeam
                {
                    Name = "kestrel",
                    TypeList = ["CORE", "DUO"],
                    Units = [new FireteamUnitLimit { Name = "ORC", Slug = "orc", Min = 0, Max = 3, MinIsAsterisk = true }]
                }
            ]
        };

        var teams = new Dictionary<string, MergedFireteam>(StringComparer.OrdinalIgnoreCase);
        MergedFireteam.MergeChart(first, teams);
        MergedFireteam.MergeChart(second, teams);
        MergedFireteam.MergeChart(null, teams);

        var team = Assert.Single(teams.Values);
        Assert.Equal(("Kestrel", 1, 1, 2), (team.Name, team.Duo, team.Haris, team.Core));
        Assert.Equal(new MergedUnitLimit(0, 3, "orc", true), Assert.Single(team.UnitLimits).Value);
    }
}
