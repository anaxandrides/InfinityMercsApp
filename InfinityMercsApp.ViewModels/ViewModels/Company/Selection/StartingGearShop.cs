using InfinityMercsApp.Domain.SpecOps;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Dialogs;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>Spends a trooper's one-off credits one item at a time, until they run out or the player is done.</summary>
public sealed class StartingGearShop(IDialogService dialogs)
{
    public const string Done = "Done";

    /// <summary>
    /// The gear bought: each pick from what is affordable and not yet bought. Whatever is left when the player is done
    /// is lost.
    /// </summary>
    public async Task<IReadOnlyList<TrooperUpgrade>> BuyAsync(string trooperName, int credits, IReadOnlyList<StartingGearOption> options)
    {
        var bought = new List<StartingGearOption>();
        var left = credits;
        while (true)
        {
            var affordable = options.Where(option => option.Cost <= left && !bought.Contains(option)).ToList();
            if (affordable.Count == 0)
            {
                break;
            }

            var labels = affordable.Select(option => option.Label).ToList();
            var choice = await dialogs.ChooseAsync(
                $"{trooperName}: {left} of {credits} starting credits left (unspent credits are lost)", Done, labels);
            var picked = choice is null ? -1 : labels.IndexOf(choice);
            if (picked < 0)
            {
                break;
            }

            bought.Add(affordable[picked]);
            left -= affordable[picked].Cost;
        }

        return bought.Select(option => option.Upgrade).ToList();
    }
}
