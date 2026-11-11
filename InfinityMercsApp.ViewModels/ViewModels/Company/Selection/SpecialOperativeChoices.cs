using InfinityMercsApp.Domain.SpecOps;
using InfinityMercsApp.Services.CompanySelection.SpecOps;
using InfinityMercsApp.Services.Dialogs;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>Asks for a Spec-Ops or Team-Ops trooper's upgrades, one list at a time, and for a Spec-Ops's SpecBot.</summary>
public sealed class SpecialOperativeChoices(IDialogService dialogs)
{
    /// <summary>
    /// <paramref name="count"/> different upgrades from <paramref name="table"/> (fewer when it has fewer), each picked
    /// from those not yet taken.
    /// </summary>
    /// <returns>The upgrades in the order picked; null when the player backed out.</returns>
    public async Task<IReadOnlyList<TrooperUpgrade>?> ChooseUpgradesAsync(string trooperName, IReadOnlyList<TrooperUpgrade> table, int count)
    {
        var total = Math.Min(count, table.Count);
        var taken = new List<int>();
        while (taken.Count < total)
        {
            var left = Enumerable.Range(0, table.Count).Where(index => !taken.Contains(index)).ToList();
            var labels = Labels(left.Select(index => table[index].Label).ToList());
            var title = total == 1 ? $"{trooperName}: choose an upgrade" : $"{trooperName}: upgrade {taken.Count + 1} of {total}";
            var choice = await dialogs.ChooseAsync(title, "Cancel", labels);
            var picked = choice is null ? -1 : labels.IndexOf(choice);
            if (picked < 0)
            {
                return null;
            }

            taken.Add(left[picked]);
        }

        return taken.Select(index => table[index]).ToList();
    }

    /// <summary>The SpecBot the player brings; null for none (or when there is none to bring).</summary>
    public async Task<SpecBotOption?> ChooseSpecBotAsync(string trooperName, IReadOnlyList<SpecBotOption> options)
    {
        if (options.Count == 0)
        {
            return null;
        }

        var labels = Labels(options.Select(option => option.Label).ToList());
        var choice = await dialogs.ChooseAsync($"Does {trooperName} bring a SpecBot?", "No SpecBot", labels);
        var picked = choice is null ? -1 : labels.IndexOf(choice);
        return picked >= 0 ? options[picked] : null;
    }

    // Labels as listed, numbered where two read the same so each can be told apart.
    private static List<string> Labels(IReadOnlyList<string> labels) =>
        labels
            .Select((label, index) => labels.Count(other => string.Equals(other, label, StringComparison.OrdinalIgnoreCase)) > 1
                ? $"{label} ({index + 1})"
                : label)
            .ToList();
}
