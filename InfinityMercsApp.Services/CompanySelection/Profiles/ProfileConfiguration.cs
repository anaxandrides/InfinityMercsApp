using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>
/// One configuration of a unit as a selection screen lists it: its weapons, the equipment and skills that set
/// it apart from the unit's other configurations, its peripheral, SWC and cost.
/// </summary>
/// <param name="ForcedLieutenant">Every configuration is a lieutenant (a Spec-Ops unit), so its lieutenant skill is highlighted.</param>
/// <param name="Peripherals">The peripherals as listed, e.g. "Traktor Mul (1)".</param>
/// <param name="Peripheral">The first peripheral's statline, when the unit has its profile.</param>
/// <param name="PeripheralSkills">The peripheral's skills, with Irregular when it is controlled.</param>
/// <param name="PeripheralIsControlled">The peripheral is a synchronized, control or cyberplug one: it is Irregular and gives its controller FT Master.</param>
/// <param name="PeripheralUnitName">The peripheral's name without its count or control tag, e.g. "Traktor Mul".</param>
/// <param name="HasTacticalAwareness">The configuration has Tactical Awareness, as an order or a skill.</param>
/// <param name="GroupId">The profile group the option belongs to, whose profiles are the trooper's forms.</param>
public sealed record ProfileConfiguration(
    string GroupName,
    string Name,
    string ProfileKey,
    bool IsLieutenant,
    bool ForcedLieutenant,
    IReadOnlyList<string> RangedWeapons,
    IReadOnlyList<string> MeleeWeapons,
    IReadOnlyList<string> UniqueEquipment,
    IReadOnlyList<string> UniqueSkills,
    IReadOnlyList<string> Characteristics,
    IReadOnlyList<string> Peripherals,
    PeripheralMercsCompanyStats? Peripheral,
    string PeripheralEquipment,
    string PeripheralSkills,
    bool PeripheralIsControlled,
    string PeripheralUnitName,
    string Swc,
    string Cost,
    bool HasTacticalAwareness,
    int? GroupId);
