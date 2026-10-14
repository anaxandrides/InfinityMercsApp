using InfinityMercsApp.Views.Controls.UnitFilter;

namespace InfinityMercsApp.Services.CompanySelection.Core;

public sealed class CompanySelectionFilterState
{
    public UnitFilterCriteria ActiveUnitFilter { get; set; } = UnitFilterCriteria.None;
    public UnitFilterPopupView? ActiveUnitFilterPopup { get; set; }
    public UnitFilterPopupOptions? PreparedUnitFilterPopupOptions { get; set; }
}
