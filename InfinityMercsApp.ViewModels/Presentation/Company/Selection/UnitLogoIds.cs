namespace InfinityMercsApp.Presentation.Company.Selection;

/// <summary>The faction and unit whose logo a unit shows; generated company units borrow their source unit's.</summary>
public sealed record UnitLogoIds(int FactionId, int UnitId);
