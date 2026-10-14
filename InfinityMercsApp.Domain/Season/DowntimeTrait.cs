namespace InfinityMercsApp.Domain.Season;

/// <summary>
/// What a choice does: gains, losses and requirements. <see cref="Detail"/> names the skill or requirement
/// (or is the text of a note); a negated trait only applies when the test fails.
/// </summary>
public sealed record DowntimeTrait(TraitKind Kind, string? Detail = null, bool Negated = false)
{
    public string Display
    {
        get
        {
            var label = Kind switch
            {
                TraitKind.Cr => "CR",
                TraitKind.Xp => "XP",
                TraitKind.Swc => "SWC",
                TraitKind.Lt => "LT",
                TraitKind.Note => Detail ?? string.Empty,
                _ => Kind.ToString()
            };
            if (Kind is TraitKind.Skill or TraitKind.Requirement && !string.IsNullOrWhiteSpace(Detail))
            {
                label += $" ({Detail})";
            }

            return Negated ? $"{label} (Neg)" : label;
        }
    }
}
