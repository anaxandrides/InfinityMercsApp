namespace InfinityMercsApp.Domain.Captain;

/// <summary>
/// A captain's weapon, skill or equipment slots: the labels on offer, such as "(2) - Sniper Rifle", and the
/// label chosen in each slot (<see cref="CaptainChoiceLabel.None"/> for an empty one).
/// </summary>
public sealed record CaptainChoicePicks(string Group, IReadOnlyList<string> Offered, IReadOnlyList<string> Chosen)
{
    /// <summary>The labels chosen in slots that are not empty.</summary>
    public IReadOnlyList<string> Filled => Chosen.Where(label => CaptainChoiceLabel.Name(label).Length > 0).ToList();

    public int Cost => Filled.Sum(CaptainChoiceLabel.Cost);
}
