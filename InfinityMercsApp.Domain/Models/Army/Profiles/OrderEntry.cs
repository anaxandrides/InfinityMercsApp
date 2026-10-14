using InfinityMercsApp.Domain.Models.Army.Traits;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// An order an option provides (REGULAR, IRREGULAR, TACTICAL, LIEUTENANT...).
/// </summary>
public sealed record OrderEntry(string? Type, int? List, int? Total)
{
    public bool IsLieutenant => string.Equals(Type, OrderTypes.Lieutenant, StringComparison.OrdinalIgnoreCase);
}
