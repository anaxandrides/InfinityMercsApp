using System.Text.Json;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Services.Season;

/// <summary>
/// A Proxy Pack's Aspects: the Lone Wolf copied into more trooper slots, each with a loadout of its own while the
/// stats, skills, perks, XP and injuries stay the Lone Wolf's. The Lone Wolf's own loadout is the first Aspect; more
/// are saved as entries pointing at it as its rank allows, and never taken away. Each is a copy of the Lone Wolf's
/// saved line, made again whenever the company is saved, so a change to the Lone Wolf carries over; an Aspect's own
/// loadout is season gear under its own entry, which the copy never touches.
/// </summary>
public static class LoneWolfAspects
{
    /// <summary>The Lone Wolf of a Proxy Pack; null for any other company.</summary>
    public static SavedCompanyEntry? LoneWolfOf(SavedCompanyFile company) =>
        company.Entries.FirstOrDefault(entry => entry.IsLoneWolf);

    /// <summary>
    /// Adds the Aspects the Lone Wolf's rank allows and the company doesn't have yet, and brings every Aspect in line
    /// with the Lone Wolf. True when anything changed, so the company should be saved.
    /// </summary>
    public static bool Sync(SavedCompanyFile company, SeasonFile? season)
    {
        if (LoneWolfOf(company) is not { } wolf)
        {
            return false;
        }

        var wanted = SeasonTroopers.ProgressOf(wolf, season).Aspects;
        var have = 1 + AspectsOf(company, wolf).Count;
        for (var number = have + 1; number <= wanted; number++)
        {
            company.Entries.Add(AspectOf(wolf, number, company.Entries.Max(entry => entry.EntryIndex) + 1));
        }

        return Refresh(company) || wanted > have;
    }

    /// <summary>
    /// Copies the Lone Wolf's saved line onto each of its Aspects again, keeping their entries and numbers. True when
    /// any Aspect changed.
    /// </summary>
    public static bool Refresh(SavedCompanyFile company)
    {
        if (LoneWolfOf(company) is not { } wolf)
        {
            return false;
        }

        var changed = false;
        var aspects = AspectsOf(company, wolf);
        for (var i = 0; i < aspects.Count; i++)
        {
            var fresh = AspectOf(wolf, number: i + 2, aspects[i].EntryIndex);
            if (JsonSerializer.Serialize(fresh) != JsonSerializer.Serialize(aspects[i]))
            {
                company.Entries[company.Entries.IndexOf(aspects[i])] = fresh;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>The name an Aspect goes by, e.g. "Kestrel (Aspect 2)".</summary>
    public static string AspectName(string loneWolfName, int number) => $"{loneWolfName} (Aspect {number})";

    // The Lone Wolf's Aspects in the order they were gained.
    private static List<SavedCompanyEntry> AspectsOf(SavedCompanyFile company, SavedCompanyEntry wolf) =>
        company.Entries.Where(entry => entry.AspectOf == wolf.EntryIndex).OrderBy(entry => entry.EntryIndex).ToList();

    // A copy of the Lone Wolf's saved line as Aspect number, costing nothing: its CR counts once, for the Lone Wolf.
    private static SavedCompanyEntry AspectOf(SavedCompanyEntry wolf, int number, int entryIndex)
    {
        var copy = JsonSerializer.SerializeToNode(wolf)?.AsObject() ?? [];
        copy[nameof(SavedCompanyEntry.EntryIndex)] = entryIndex;
        copy[nameof(SavedCompanyEntry.CustomName)] = AspectName(SeasonTroopers.NameOf(wolf), number);
        copy[nameof(SavedCompanyEntry.Cost)] = 0;
        copy[nameof(SavedCompanyEntry.IsLieutenant)] = false;
        copy[nameof(SavedCompanyEntry.IsLoneWolf)] = false;
        copy[nameof(SavedCompanyEntry.AspectOf)] = wolf.EntryIndex;
        return copy.Deserialize<SavedCompanyEntry>() ?? new SavedCompanyEntry { EntryIndex = entryIndex, AspectOf = wolf.EntryIndex };
    }
}
