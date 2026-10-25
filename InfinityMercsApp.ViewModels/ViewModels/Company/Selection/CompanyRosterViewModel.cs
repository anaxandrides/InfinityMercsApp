using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// The company being built: its name and its troopers. While recruiting, the company's own troopers are on the
/// roster too; they count for AVA and the company's rules but can't be removed.
/// </summary>
public sealed partial class CompanyRosterViewModel(IDialogService dialogs) : ObservableObject
{
    [ObservableProperty]
    public partial string CompanyName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowCompanyNameValidationError { get; set; }

    public ObservableCollection<MercsCompanyEntry> Entries { get; } = [];

    /// <summary>What the company's troopers cost.</summary>
    public int Cost => Entries.Sum(entry => entry.CostValue);

    public bool HasLieutenant => Entries.Any(entry => entry.IsLieutenant);

    /// <summary>The troopers being hired, not already on the company's roster.</summary>
    public IReadOnlyList<MercsCompanyEntry> Recruits => Entries.Where(entry => !entry.IsOnRoster).ToList();

    partial void OnCompanyNameChanged(string value)
    {
        if (ShowCompanyNameValidationError && !string.IsNullOrWhiteSpace(value))
        {
            ShowCompanyNameValidationError = false;
        }
    }

    /// <summary>Lists a recruiting company's own troopers in place of anything listed before.</summary>
    public void ShowCompany(IEnumerable<MercsCompanyEntry> onRoster)
    {
        Entries.Clear();
        foreach (var entry in onRoster)
        {
            Entries.Add(entry);
        }
    }

    /// <summary>Adds a trooper: a lieutenant first, a Servant after its owner, anyone else last.</summary>
    public void Add(MercsCompanyEntry entry, MercsCompanyEntry? servantOwner)
    {
        if (entry.IsLieutenant)
        {
            Entries.Insert(0, entry);
        }
        else if (servantOwner is not null)
        {
            Entries.Insert(Entries.IndexOf(servantOwner) + 1, entry);
        }
        else
        {
            Entries.Add(entry);
        }
    }

    /// <summary>Takes out a trooper with any Servant that belongs to it; false for one already on the company's roster.</summary>
    public bool Remove(MercsCompanyEntry entry)
    {
        if (entry.IsOnRoster || !Entries.Remove(entry))
        {
            return false;
        }

        foreach (var servant in Entries.Where(candidate => candidate.ServantOwner == entry).ToList())
        {
            Entries.Remove(servant);
        }

        return true;
    }

    /// <summary>Swaps an entry for its finished version (a TAG after its Spec Ops are chosen); false when it is gone.</summary>
    public bool Replace(MercsCompanyEntry original, MercsCompanyEntry replacement)
    {
        var index = Entries.IndexOf(original);
        if (index < 0)
        {
            return false;
        }

        Entries[index] = replacement;
        return true;
    }

    /// <summary>Takes out every recruit, keeping a recruiting company's own troopers.</summary>
    public void RemoveRecruits()
    {
        foreach (var entry in Recruits)
        {
            Entries.Remove(entry);
        }
    }

    /// <summary>Re-formats every trooper's MOV after the inches/centimetres preference changed.</summary>
    public void RefreshMoves(bool showInches)
    {
        foreach (var entry in Entries)
        {
            entry.RefreshMoves(showInches);
        }
    }

    /// <summary>
    /// A Servant recruited on its own belongs to a Doctor or Engineer in the company: the only one, or the one the
    /// player picks; null when there is none or they back out.
    /// </summary>
    public async Task<MercsCompanyEntry?> ChooseServantOwnerAsync(string servantName)
    {
        var owners = Entries.Where(entry => entry.CanOwnServant).ToList();
        switch (owners.Count)
        {
            case 0:
                await dialogs.ShowAlertAsync(
                    "Needs a Doctor or Engineer",
                    $"{servantName} must belong to a Doctor or Engineer in the company. Add one first.");
                return null;
            case 1:
                return owners[0];
        }

        var labels = owners
            .Select((owner, index) => owners.Count(other => other.Name == owner.Name) > 1 ? $"{owner.Name} ({index + 1})" : owner.Name)
            .ToList();
        var choice = await dialogs.ChooseAsync($"Who does {servantName} belong to?", "Cancel", labels);
        var chosen = choice is null ? -1 : labels.IndexOf(choice);
        return chosen >= 0 ? owners[chosen] : null;
    }
}
