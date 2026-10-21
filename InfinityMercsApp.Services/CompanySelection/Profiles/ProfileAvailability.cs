namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>Whether a profile of the picked unit is listed, and whether it can be added.</summary>
/// <param name="IsBlocked">Listed but not addable: a second lieutenant, over the points left, or past the unit's AVA.</param>
public sealed record ProfileAvailability(bool IsVisible, bool IsBlocked)
{
    /// <summary>
    /// A profile is listed when it fits the points left and the filter's points (and is a lieutenant, with
    /// <paramref name="lieutenantOnly"/>).
    /// </summary>
    public static ProfileAvailability Of(int cost, bool isLieutenant, RosterRoom room, UnitFilterCriteria filter, bool lieutenantOnly)
    {
        var overPoints = cost > room.PointsRemaining;
        var outsideFilter = (filter.MinPoints is int min && cost < min) || (filter.MaxPoints is int max && cost > max);
        return new ProfileAvailability(
            IsVisible: !(lieutenantOnly && !isLieutenant) && !overPoints && !outsideFilter,
            IsBlocked: (room.HasLieutenant && isLieutenant) || overPoints || room.AvaReached);
    }
}
