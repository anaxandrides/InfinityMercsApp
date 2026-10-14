namespace InfinityMercsApp.Services.CompanySelection.Core;

public interface ICompanySelectionVisibilityState
{
    string SelectedStartSeasonPoints { get; }
    string SeasonPointsCapText { get; }
    bool LieutenantOnlyUnits { get; }
    UnitFilterCriteria ActiveUnitFilter { get; }
}
