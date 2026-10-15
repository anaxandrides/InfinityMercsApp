namespace InfinityMercsApp.Domain.Season.Troopers;

/// <summary>
/// How a trooper differs from its saved profile: attribute changes, movement (in game inches), silhouette,
/// steps along the wounds-skill path, and skills or characteristics gained and lost. Injuries, perks, rank
/// skills and services each give one; they add together.
/// </summary>
public sealed record TrooperChange
{
    public static TrooperChange None { get; } = new();

    public int Cc { get; init; }
    public int Bs { get; init; }
    public int Ph { get; init; }
    public int Wip { get; init; }
    public int Arm { get; init; }
    public int Bts { get; init; }

    /// <summary>Change to the first MOV value, in game inches.</summary>
    public int FirstMoveInches { get; init; }

    /// <summary>Change to the second MOV value, in game inches.</summary>
    public int SecondMoveInches { get; init; }

    /// <summary>A MOV that replaces the profile's before any inch changes, e.g. 6-4.</summary>
    public MoveInches? Move { get; init; }

    /// <summary>A silhouette that replaces the profile's.</summary>
    public int? Silhouette { get; init; }

    /// <summary>Steps along Dogged → NWI → +1 VITA/STR; negative steps go back down.</summary>
    public int WoundsSkillSteps { get; init; }

    public IReadOnlyList<string> SkillsGained { get; init; } = [];

    public IReadOnlyList<string> CharacteristicsGained { get; init; } = [];

    public IReadOnlyList<string> CharacteristicsLost { get; init; } = [];

    /// <summary>Effects with no field of their own, e.g. "-2 on Discover rolls".</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    public static TrooperChange operator +(TrooperChange a, TrooperChange b) => new()
    {
        Cc = a.Cc + b.Cc,
        Bs = a.Bs + b.Bs,
        Ph = a.Ph + b.Ph,
        Wip = a.Wip + b.Wip,
        Arm = a.Arm + b.Arm,
        Bts = a.Bts + b.Bts,
        FirstMoveInches = a.FirstMoveInches + b.FirstMoveInches,
        SecondMoveInches = a.SecondMoveInches + b.SecondMoveInches,
        Move = b.Move ?? a.Move,
        Silhouette = b.Silhouette ?? a.Silhouette,
        WoundsSkillSteps = a.WoundsSkillSteps + b.WoundsSkillSteps,
        SkillsGained = [.. a.SkillsGained, .. b.SkillsGained],
        CharacteristicsGained = [.. a.CharacteristicsGained, .. b.CharacteristicsGained],
        CharacteristicsLost = [.. a.CharacteristicsLost, .. b.CharacteristicsLost],
        Notes = [.. a.Notes, .. b.Notes]
    };

    public static TrooperChange Sum(IEnumerable<TrooperChange> changes) => changes.Aggregate(None, (total, change) => total + change);
}
