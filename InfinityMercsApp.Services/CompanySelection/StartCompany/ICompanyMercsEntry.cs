namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

public interface ICompanyMercsEntry
{
    string Name { get; }
    string BaseUnitName { get; }
    int CostValue { get; }
    bool IsLieutenant { get; }
    bool IsSpecOps { get; }
    string UnitTypeCode { get; }
    string ProfileKey { get; }
    int SourceFactionId { get; }
    int SourceUnitId { get; }
    int LogoSourceFactionId { get; }
    int LogoSourceUnitId { get; }
    string SavedEquipment { get; }
    string SavedSkills { get; }
    string SavedCharacteristics { get; }
    string SavedRangedWeapons { get; }
    string SavedCcWeapons { get; }
    bool HasPeripheralStatBlock { get; }
    string PeripheralNameHeading { get; }
    string PeripheralMov { get; set; }
    string PeripheralCc { get; }
    string PeripheralBs { get; }
    string PeripheralPh { get; }
    string PeripheralWip { get; }
    string PeripheralArm { get; }
    string PeripheralBts { get; }
    string PeripheralVitalityHeader { get; }
    string PeripheralVitality { get; }
    string PeripheralS { get; }
    string PeripheralAva { get; }
    string SavedPeripheralEquipment { get; }
    string SavedPeripheralSkills { get; }
    string SavedPeripheralCharacteristics { get; }
    string SavedPeripheralRangedWeapons { get; }
    string SavedPeripheralCcWeapons { get; }
    int ExperiencePoints { get; }
    List<CompanyTrooperPerkState> Perks { get; }

    /// <summary>What a TAG company TAG bought in its Spec Ops popup, e.g. "Spec-Ops: +1 ARM".</summary>
    IReadOnlyList<string> SpecOpsPurchases { get; }
    int? UnitMoveFirstCm { get; }
    int? UnitMoveSecondCm { get; }
    string UnitMoveDisplay { get; set; }
    string? Subtitle { get; set; }
    int? PeripheralMoveFirstCm { get; }
    int? PeripheralMoveSecondCm { get; }
    string? CachedLogoPath { get; }
    string? PackagedLogoPath { get; }

    /// <summary>
    /// The Doctor or Engineer a Servant recruited on its own belongs to: it is saved as that trooper's peripheral,
    /// taking no slot. Null for every other trooper.
    /// </summary>
    ICompanyMercsEntry? ServantOwner { get; }

    /// <summary>Where the trooper is in the saved company, for one already on a recruiting company's roster.</summary>
    int? SavedEntryIndex { get; }
}
