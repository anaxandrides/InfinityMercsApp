
namespace InfinityMercsApp.Domain.Models.Filtering;

// Unit list filter criteria chosen in the filter popup. Moved from Views/Controls/UnitFilterPopupView
// so the filter rules can be evaluated (and tested) against the domain profile models.

public sealed class UnitFilterCriteria
{
    public IReadOnlyList<UnitFilterTerm> Terms { get; init; } = [];
    public int? MinPoints { get; init; }
    public int? MaxPoints { get; init; }
    public bool LieutenantOnlyUnits { get; init; }
    public bool TeamsView { get; init; }

    public static UnitFilterCriteria None { get; } = new();

    public bool IsActive => Terms.Count > 0
        || (MinPoints.HasValue && MinPoints.Value != 0)
        || (MaxPoints.HasValue && MaxPoints.Value != 200)
        || LieutenantOnlyUnits;

    public UnitFilterQuery ToQuery()
    {
        return UnitFilterQuery.FromCriteria(this);
    }
}
