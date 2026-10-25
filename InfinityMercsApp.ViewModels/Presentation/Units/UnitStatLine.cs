using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Presentation.Units;

/// <summary>A statline as the unit display shows it; every value is display text, "-" when unknown.</summary>
public sealed record UnitStatLine
{
    public string Mov { get; init; } = "-";
    public string Cc { get; init; } = "-";
    public string Bs { get; init; } = "-";
    public string Ph { get; init; } = "-";
    public string Wip { get; init; } = "-";
    public string Arm { get; init; } = "-";
    public string Bts { get; init; } = "-";

    /// <summary>VITA, or STR for structure troopers.</summary>
    public string VitalityHeader { get; init; } = Stat.Vita.Key();

    public string Vitality { get; init; } = "-";

    /// <summary>A profile's statline, its MOV in inches or centimetres.</summary>
    public static UnitStatLine From(UnitProfile profile, bool showUnitsInInches)
    {
        var vitality = profile.Vitality;
        return new UnitStatLine
        {
            Mov = Movement.Format(profile.Movement, showUnitsInInches),
            Cc = UnitProfile.StatText(profile.Cc),
            Bs = UnitProfile.StatText(profile.Bs),
            Ph = UnitProfile.StatText(profile.Ph),
            Wip = UnitProfile.StatText(profile.Wip),
            Arm = UnitProfile.StatText(profile.Arm),
            Bts = UnitProfile.StatText(profile.Bts),
            S = UnitProfile.StatText(profile.S),
            Ava = profile.AvaText,
            VitalityHeader = vitality.Stat.Key(),
            Vitality = vitality.Value
        };
    }
    public string S { get; init; } = "-";
    public string Ava { get; init; } = "-";

    /// <summary>No unit: every value "-".</summary>
    public static UnitStatLine Empty { get; } = new();
}
