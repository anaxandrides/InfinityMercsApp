using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

/// <summary>A company entry as it is written to the company file.</summary>
internal sealed class SerializedCompanyEntry
{
    public int EntryIndex { get; init; }
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;
    [JsonPropertyName("Base Profile (Human Readable)")]
    public string BaseProfileHumanReadable { get; init; } = string.Empty;
    [JsonPropertyName("IsCaptain")]
    public bool IsCaptain { get; init; }
    [JsonPropertyName("FactionId")]
    public int FactionId { get; init; }
    [JsonPropertyName("ProfileId")]
    public int ProfileId { get; init; }
    [JsonPropertyName("Renown")]
    public int Renown { get; init; }
    [JsonPropertyName("Base CR")]
    public int BaseCr { get; init; }
    [JsonPropertyName("Calculated CR")]
    public int CalculatedCr { get; init; }
    [JsonPropertyName("Base MOV")]
    public string BaseMov { get; init; } = "-";
    [JsonPropertyName("Base CC")]
    public string BaseCc { get; init; } = "-";
    [JsonPropertyName("Base BS")]
    public string BaseBs { get; init; } = "-";
    [JsonPropertyName("Base PH")]
    public string BasePh { get; init; } = "-";
    [JsonPropertyName("Base WIP")]
    public string BaseWip { get; init; } = "-";
    [JsonPropertyName("Base ARM")]
    public string BaseArm { get; init; } = "-";
    [JsonPropertyName("Base BTS")]
    public string BaseBts { get; init; } = "-";
    [JsonPropertyName("Base VITA/STR")]
    public string BaseVitaOrStr { get; init; } = "-";
    [JsonPropertyName("Base S")]
    public string BaseS { get; init; } = "-";
    [JsonPropertyName("Current MOV")]
    public string CurrentMov { get; init; } = "-";
    [JsonPropertyName("CurrentMoveFirstCm")]
    public int? CurrentMoveFirstCm { get; init; }
    [JsonPropertyName("CurrentMoveSecondCm")]
    public int? CurrentMoveSecondCm { get; init; }
    [JsonPropertyName("Current CC")]
    public string CurrentCc { get; init; } = "-";
    [JsonPropertyName("Current BS")]
    public string CurrentBs { get; init; } = "-";
    [JsonPropertyName("Current PH")]
    public string CurrentPh { get; init; } = "-";
    [JsonPropertyName("Current WIP")]
    public string CurrentWip { get; init; } = "-";
    [JsonPropertyName("Current ARM")]
    public string CurrentArm { get; init; } = "-";
    [JsonPropertyName("Current BTS")]
    public string CurrentBts { get; init; } = "-";
    [JsonPropertyName("Current VITA/STR")]
    public string CurrentVitaOrStr { get; init; } = "-";
    [JsonPropertyName("Current S")]
    public string CurrentS { get; init; } = "-";
    [JsonPropertyName("Perks")]
    public List<CompanyTrooperPerkState> Perks { get; init; } = [];
    [JsonPropertyName("XP")]
    public int Xp { get; init; }
    [JsonPropertyName("BaseSkillCodes")]
    public List<CompanySavedCodeRef> BaseSkillCodes { get; init; } = [];
    [JsonPropertyName("CurrentSkillCodes")]
    public List<CompanySavedCodeRef> CurrentSkillCodes { get; init; } = [];
    [JsonPropertyName("BaseCharacteristicCodes")]
    public List<CompanySavedCodeRef> BaseCharacteristicCodes { get; init; } = [];
    [JsonPropertyName("CurrentCharacteristicCodes")]
    public List<CompanySavedCodeRef> CurrentCharacteristicCodes { get; init; } = [];
    [JsonPropertyName("BaseEquipmentCodes")]
    public List<CompanySavedCodeRef> BaseEquipmentCodes { get; init; } = [];
    [JsonPropertyName("CurrentEquipmentCodes")]
    public List<CompanySavedCodeRef> CurrentEquipmentCodes { get; init; } = [];
    [JsonPropertyName("BaseWeaponCodes")]
    public List<CompanySavedCodeRef> BaseWeaponCodes { get; init; } = [];
    [JsonPropertyName("CurrentWeaponCodes")]
    public List<CompanySavedCodeRef> CurrentWeaponCodes { get; init; } = [];
    [JsonPropertyName("Custom Weapons")]
    public List<string> CustomWeapons { get; init; } = [];
    [JsonPropertyName("SpecOpsPurchases")]
    public List<string> SpecOpsPurchases { get; init; } = [];
    [JsonPropertyName("Custom Skills")]
    public List<string> CustomSkills { get; init; } = [];
    [JsonPropertyName("Custom Characteristics")]
    public List<string> CustomCharacteristics { get; init; } = [];
    [JsonPropertyName("Custom Equipment")]
    public List<string> CustomEquipment { get; init; } = [];
    [JsonPropertyName("Rank1Skill")]
    public string Rank1Skill { get; init; } = string.Empty;
    [JsonPropertyName("Rank2Skill")]
    public string Rank2Skill { get; init; } = string.Empty;
    [JsonPropertyName("Rank3Skill")]
    public string Rank3Skill { get; init; } = string.Empty;
    [JsonPropertyName("Rank4Skill")]
    public string Rank4Skill { get; init; } = string.Empty;
    [JsonPropertyName("Rank5Skill")]
    public string Rank5Skill { get; init; } = string.Empty;
    [JsonPropertyName("Rank6Skill")]
    public string Rank6Skill { get; init; } = string.Empty;
    [JsonPropertyName("Rank7Skill")]
    public string Rank7Skill { get; init; } = string.Empty;
    [JsonPropertyName("EquippedPrimary")]
    public string EquippedPrimary { get; init; } = string.Empty;
    [JsonPropertyName("EquippedSecondary")]
    public string EquippedSecondary { get; init; } = string.Empty;
    [JsonPropertyName("EquippedSideArm")]
    public string EquippedSideArm { get; init; } = string.Empty;
    [JsonPropertyName("EquippedAccessories")]
    public string EquippedAccessories { get; init; } = string.Empty;
    [JsonPropertyName("EquippedRoles")]
    public string EquippedRoles { get; init; } = string.Empty;
    [JsonPropertyName("EquippedArmor")]
    public string EquippedArmor { get; init; } = string.Empty;
    [JsonPropertyName("EquippedAugment")]
    public string EquippedAugment { get; init; } = string.Empty;
    [JsonPropertyName("EquippedOther")]
    public string EquippedOther { get; init; } = string.Empty;

    public string BaseUnitName { get; init; } = string.Empty;
    public string CustomName { get; init; } = string.Empty;
    public string UnitTypeCode { get; init; } = string.Empty;
    public string ProfileKey { get; init; } = string.Empty;
    public int SourceFactionId { get; init; }
    public int SourceUnitId { get; init; }
    public int LogoSourceFactionId { get; init; }
    public int LogoSourceUnitId { get; init; }
    public bool IsPeripheralUnit { get; init; }
    public int? ParentEntryIndex { get; init; }
    public int Cost { get; init; }
    public bool IsLieutenant { get; init; }
    public bool CarriesCaptainUpgrades { get; init; }
    public bool IsLoneWolf { get; init; }
    [JsonIgnore]
    public bool HasPeripheralStatBlock { get; init; }
    [JsonIgnore]
    public string PeripheralNameHeading { get; init; } = string.Empty;
    [JsonIgnore]
    public string PeripheralMov { get; init; } = "-";
    [JsonIgnore]
    public string PeripheralCc { get; init; } = "-";
    [JsonIgnore]
    public string PeripheralBs { get; init; } = "-";
    [JsonIgnore]
    public string PeripheralPh { get; init; } = "-";
    [JsonIgnore]
    public string PeripheralWip { get; init; } = "-";
    [JsonIgnore]
    public string PeripheralArm { get; init; } = "-";
    [JsonIgnore]
    public string PeripheralBts { get; init; } = "-";
    [JsonIgnore]
    public string PeripheralVitalityHeader { get; init; } = Stat.Vita.Key();
    [JsonIgnore]
    public string PeripheralVitality { get; init; } = "-";
    [JsonIgnore]
    public string PeripheralS { get; init; } = "-";
    [JsonIgnore]
    public string PeripheralAva { get; init; } = "-";
    [JsonIgnore]
    public string SavedPeripheralEquipment { get; init; } = "-";
    [JsonIgnore]
    public string SavedPeripheralSkills { get; init; } = "-";
    public int ExperiencePoints { get; init; }
}
