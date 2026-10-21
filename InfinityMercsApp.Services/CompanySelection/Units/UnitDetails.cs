using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Traits;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>
/// What a selection screen shows for the unit picked: its profiles and the filters naming their entries, its
/// trait icons, and the equipment and skills every listed profile shares.
/// </summary>
/// <param name="Unit">The unit's army record; null for a Spec-Ops unit the faction's units don't list.</param>
/// <param name="TreatAsSpecOps">Whether the unit is shown as Spec-Ops: lieutenant skill kept, every option listed.</param>
public sealed record UnitDetails(
    ArmyUnitRecord? Unit,
    UnitProfileGroups ProfileGroups,
    ArmyFilterCatalog Filters,
    bool TreatAsSpecOps,
    UnitOrderTraits OrderTraits,
    UnitTechTraits TechTraits,
    IReadOnlyList<string> CommonEquipment,
    IReadOnlyList<string> CommonSkills);
