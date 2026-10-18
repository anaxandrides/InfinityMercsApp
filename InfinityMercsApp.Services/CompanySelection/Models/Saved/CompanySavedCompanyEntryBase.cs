using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.RecruitRolls;

namespace InfinityMercsApp.Services.CompanySelection.Models.Saved;

public abstract class CompanySavedCompanyEntryBase
{
    public int EntryIndex { get; init; }
    public string Name { get; init; } = string.Empty;
    public string BaseUnitName { get; set; } = string.Empty;
    public string CustomName { get; set; } = string.Empty;
    public string UnitTypeCode { get; set; } = string.Empty;
    public string ProfileKey { get; init; } = string.Empty;
    public int SourceFactionId { get; init; }
    public int SourceUnitId { get; init; }
    public int LogoSourceFactionId { get; init; }
    public int LogoSourceUnitId { get; init; }
    public bool IsPeripheralUnit { get; init; }
    /// <summary>The trooper a peripheral is connected to; null for a controlled peripheral no trooper controls now.</summary>
    public int? ParentEntryIndex { get; set; }

    /// <summary>
    /// A peripheral added by the Peripheral (Controller) augment. The company owns it: it moves to whoever equips the
    /// augment next, and keeps its XP, perks and injuries.
    /// </summary>
    public bool FromControllerAugment { get; init; }
    public int Cost { get; init; }
    /// <summary>The Captain; a downtime change of leadership can pass the role to another trooper.</summary>
    public bool IsLieutenant { get; set; }

    /// <summary>
    /// Carries the captain upgrades bought when the company was founded: the founding Captain, who keeps them when
    /// another trooper becomes Captain.
    /// </summary>
    public bool CarriesCaptainUpgrades { get; init; }

    /// <summary>A Proxy Pack's Lone Wolf: two perks and 10 renown a rank, and up to four Aspects.</summary>
    public bool IsLoneWolf { get; init; }

    [JsonIgnore]
    public string SavedEquipment { get; init; } = "-";
    [JsonIgnore]
    public string SavedSkills { get; init; } = "-";
    [JsonIgnore]
    public string SavedRangedWeapons { get; init; } = "-";
    [JsonIgnore]
    public string SavedCcWeapons { get; init; } = "-";

    [JsonPropertyName("CurrentSkillCodes")]
    public List<CompanySavedCodeRef> CurrentSkillCodes { get; init; } = [];
    [JsonPropertyName("CurrentCharacteristicCodes")]
    public List<CompanySavedCodeRef> CurrentCharacteristicCodes { get; init; } = [];
    [JsonPropertyName("CurrentEquipmentCodes")]
    public List<CompanySavedCodeRef> CurrentEquipmentCodes { get; init; } = [];
    [JsonPropertyName("CurrentWeaponCodes")]
    public List<CompanySavedCodeRef> CurrentWeaponCodes { get; init; } = [];
    [JsonPropertyName("Custom Skills")]
    public List<string> CustomSkills { get; init; } = [];
    [JsonPropertyName("Custom Characteristics")]
    public List<string> CustomCharacteristics { get; init; } = [];
    [JsonPropertyName("Custom Equipment")]
    public List<string> CustomEquipment { get; init; } = [];
    [JsonPropertyName("Custom Weapons")]
    public List<string> CustomWeapons { get; init; } = [];

    /// <summary>The saved codes for one kind of entry (characteristics, skills, equipment or weapons).</summary>
    public IReadOnlyList<CompanySavedCodeRef> CurrentCodes(ProfileEntryKind kind) => kind switch
    {
        ProfileEntryKind.Chars => CurrentCharacteristicCodes ?? [],
        ProfileEntryKind.Skills => CurrentSkillCodes ?? [],
        ProfileEntryKind.Equip => CurrentEquipmentCodes ?? [],
        ProfileEntryKind.Weapons => CurrentWeaponCodes ?? [],
        _ => []
    };

    /// <summary>Names saved as text for one kind of entry because they had no code.</summary>
    public IReadOnlyList<string> CustomNames(ProfileEntryKind kind) => kind switch
    {
        ProfileEntryKind.Chars => CustomCharacteristics ?? [],
        ProfileEntryKind.Skills => CustomSkills ?? [],
        ProfileEntryKind.Equip => CustomEquipment ?? [],
        ProfileEntryKind.Weapons => CustomWeapons ?? [],
        _ => []
    };

    public bool HasPeripheralStatBlock { get; init; }
    public string PeripheralNameHeading { get; init; } = string.Empty;
    public string PeripheralMov { get; init; } = "-";
    public string PeripheralCc { get; init; } = "-";
    public string PeripheralBs { get; init; } = "-";
    public string PeripheralPh { get; init; } = "-";
    public string PeripheralWip { get; init; } = "-";
    public string PeripheralArm { get; init; } = "-";
    public string PeripheralBts { get; init; } = "-";
    public string PeripheralVitalityHeader { get; init; } = Stat.Vita.Key();
    public string PeripheralVitality { get; init; } = "-";
    public string PeripheralS { get; init; } = "-";
    public string PeripheralAva { get; init; } = "-";
    public string SavedPeripheralEquipment { get; init; } = "-";
    public string SavedPeripheralSkills { get; init; } = "-";
    public string SavedPeripheralCharacteristics { get; init; } = "-";
    public int ExperiencePoints { get; init; }
    public string ExperienceRankName => CompanyUnitExperienceRanks.GetRankName(ExperiencePoints);
    [JsonPropertyName("Perks")]
    public List<CompanyTrooperPerkState> Perks { get; init; } = [];

    /// <summary>What a TAG company TAG bought in its Spec Ops popup, e.g. "Spec-Ops: +1 ARM".</summary>
    [JsonPropertyName("SpecOpsPurchases")]
    public List<string> SpecOpsPurchases { get; set; } = [];

    /// <summary>The trooper's Booty and Metachemistry rolls, made once when it was bought and kept for good.</summary>
    [JsonPropertyName("RecruitRolls")]
    public List<RecruitRoll> RecruitRolls { get; set; } = [];

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
}
