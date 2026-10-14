namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// One reference from a profile or option to a lookup item (a skill, weapon, piece of equipment...).
/// The API sends these as bare ids (<c>5</c>) or as objects (<c>{"id":5,"q":2,"order":1,"extra":[7]}</c>).
/// </summary>
/// <param name="Id">The referenced lookup id, or <c>null</c> when the entry carries no usable id.</param>
/// <param name="Quantity">How many of the item the entry grants; always at least 1.</param>
/// <param name="Order">The API display order, or <c>null</c> when none was given.</param>
/// <param name="Extras">Ids of the entry's extras (distances, ammo modifiers...), in API order.</param>
public sealed record ProfileEntry(int? Id, int Quantity, int? Order, IReadOnlyList<int> Extras)
{
    /// <summary>Sort key used when ordering entries for display: unordered entries sort last.</summary>
    public int SortOrder => Order ?? int.MaxValue;

    public static ProfileEntry ForId(int id) => new(id, 1, null, []);
}
