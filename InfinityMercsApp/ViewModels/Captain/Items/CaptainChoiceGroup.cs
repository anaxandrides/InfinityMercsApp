using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.ViewModels.Captain.Items;

/// <summary>A captain's three weapon, skill or equipment slots, under one heading.</summary>
public sealed record CaptainChoiceGroup(string Title, CaptainChoiceSlot First, CaptainChoiceSlot Second, CaptainChoiceSlot Third)
{
    public IReadOnlyList<CaptainChoiceSlot> Slots => [First, Second, Third];

    /// <summary>Distinct non-empty names chosen across the slots, in slot order.</summary>
    public IReadOnlyList<string> ChosenNames => Slots
        .Select(slot => slot.ChosenName)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    public int Cost => Slots.Sum(slot => slot.Cost);

    /// <summary>What is on offer and what each slot holds, for checking the build.</summary>
    public CaptainChoicePicks Picks => new(
        Title,
        First.Options.Where(option => option != CaptainChoiceLabel.None).ToList(),
        Slots.Select(slot => slot.SelectedChoice ?? CaptainChoiceLabel.None).ToList());

    /// <summary>Three slots offering "(None)" and then <paramref name="options"/>, trimmed and without blanks or repeats.</summary>
    public static CaptainChoiceGroup For(string title, IEnumerable<string> options, Action selectionChanged)
    {
        var choices = new List<string> { CaptainChoiceLabel.None };
        choices.AddRange(options
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Select(option => option.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase));

        return new CaptainChoiceGroup(
            title,
            new CaptainChoiceSlot(choices, selectionChanged),
            new CaptainChoiceSlot(choices, selectionChanged),
            new CaptainChoiceSlot(choices, selectionChanged));
    }
}
