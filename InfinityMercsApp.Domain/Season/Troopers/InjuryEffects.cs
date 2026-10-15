namespace InfinityMercsApp.Domain.Season.Troopers;

/// <summary>What each injury is called and does to the trooper.</summary>
public static class InjuryEffects
{
    public static string Label(this Injury injury) => injury switch
    {
        Injury.BattleFury => "Battle Fury",
        Injury.PuncturedLung => "Punctured Lung",
        Injury.Arms => "Arms",
        Injury.BrainInjury => "Brain Injury",
        Injury.LegsInjury => "Legs Injury",
        Injury.BodyInjury => "Body Injury",
        Injury.EyesInjury => "Eyes Injury",
        _ => "Shell Shocked"
    };

    /// <summary>The injury's effect in words, e.g. "-1 PH, -1 to the second MOV value".</summary>
    public static string EffectText(this Injury injury) => injury switch
    {
        Injury.BattleFury => "Frenzy",
        Injury.PuncturedLung => "-1 PH, -1 to the second MOV value",
        Injury.Arms => "-1 BS",
        Injury.BrainInjury => "-1 WIP, -3 CC",
        Injury.LegsInjury => "-2 to the first MOV value",
        Injury.BodyInjury => "-2 PH",
        Injury.EyesInjury => "-1 BS, -2 on Discover rolls",
        _ => "-2 WIP"
    };

    public static TrooperChange Change(this Injury injury) => injury switch
    {
        Injury.BattleFury => new() { CharacteristicsGained = [Frenzy] },
        Injury.PuncturedLung => new() { Ph = -1, SecondMoveInches = -1 },
        Injury.Arms => new() { Bs = -1 },
        Injury.BrainInjury => new() { Wip = -1, Cc = -3 },
        Injury.LegsInjury => new() { FirstMoveInches = -2 },
        Injury.BodyInjury => new() { Ph = -2 },
        Injury.EyesInjury => new() { Bs = -1, Notes = ["-2 on Discover rolls (Eyes Injury)"] },
        _ => new() { Wip = -2 }
    };

    public const string Frenzy = "Frenzy";

    /// <summary>Reads an injury by its label or name, ignoring case and spaces.</summary>
    public static bool TryParse(string? text, out Injury injury)
    {
        var key = (text ?? string.Empty).Replace(" ", string.Empty);
        foreach (var candidate in Enum.GetValues<Injury>())
        {
            if (string.Equals(candidate.ToString(), key, StringComparison.OrdinalIgnoreCase))
            {
                injury = candidate;
                return true;
            }
        }

        injury = default;
        return false;
    }
}
