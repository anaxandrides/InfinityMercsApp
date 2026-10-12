namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// Which id list on a profile or option an entry comes from (<c>chars</c>, <c>skills</c>, <c>weapons</c>...).
/// </summary>
public enum ProfileEntryKind
{
    Chars,
    Skills,
    Equip,
    Weapons,
    Peripheral,
    Ammunition,
    Ammo
}

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

/// <summary>
/// A reference from one option to another option whose entries it also grants.
/// </summary>
public sealed record IncludeReference(int GroupId, int OptionId);

/// <summary>
/// An order an option provides (REGULAR, IRREGULAR, TACTICAL, LIEUTENANT...).
/// </summary>
public sealed record OrderEntry(string? Type, int? List, int? Total)
{
    public bool IsLieutenant => string.Equals(Type, "LIEUTENANT", StringComparison.OrdinalIgnoreCase);
}
