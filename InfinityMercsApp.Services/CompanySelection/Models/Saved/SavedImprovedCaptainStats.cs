using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Services.CompanySelection.Models.Saved;

/// <summary>
/// The captain upgrades chosen when a company is founded (or the TAG company's spec-ops build), as saved
/// in the company file and returned by the captain configuration popup.
/// </summary>
public sealed class SavedImprovedCaptainStats
{
    public bool IsEnabled { get; init; }
    public string CaptainName { get; init; } = string.Empty;
    public int CcTier { get; init; }
    public int BsTier { get; init; }
    public int PhTier { get; init; }
    public int WipTier { get; init; }
    public int ArmTier { get; init; }
    public int BtsTier { get; init; }
    public int VitalityTier { get; init; }
    public int CcBonus { get; init; }
    public int BsBonus { get; init; }
    public int PhBonus { get; init; }
    public int WipBonus { get; init; }
    public int ArmBonus { get; init; }
    public int BtsBonus { get; init; }
    public int VitalityBonus { get; init; }

    /// <summary>The bought stat bonuses as one value.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public CaptainStatBonuses Bonuses =>
        new(CcBonus, BsBonus, PhBonus, WipBonus, ArmBonus, BtsBonus, VitalityBonus);
    public string WeaponChoice1 { get; init; } = string.Empty;
    public string WeaponChoice2 { get; init; } = string.Empty;
    public string WeaponChoice3 { get; init; } = string.Empty;
    public string SkillChoice1 { get; init; } = string.Empty;
    public string SkillChoice2 { get; init; } = string.Empty;
    public string SkillChoice3 { get; init; } = string.Empty;
    public string EquipmentChoice1 { get; init; } = string.Empty;
    public string EquipmentChoice2 { get; init; } = string.Empty;
    public string EquipmentChoice3 { get; init; } = string.Empty;
    public int OptionFactionId { get; init; }
    public string OptionFactionName { get; init; } = string.Empty;

    /// <summary>
    /// What was bought with the captain's Spec-Ops XP, as an army sheet lists it among the perks: "Spec-Ops: +1 VITA",
    /// "Spec-Ops: Pistol". <paramref name="vitality"/> names VITA or STR. Nothing when there are no upgrades.
    /// </summary>
    public IReadOnlyList<string> SpecOpsPurchases(Stat vitality) =>
        !IsEnabled
            ? []
            : [
                .. Bonuses.Lines(vitality)
                    .Concat(SavedUnitText.CaptainChoiceNames(
                        WeaponChoice1, WeaponChoice2, WeaponChoice3,
                        SkillChoice1, SkillChoice2, SkillChoice3,
                        EquipmentChoice1, EquipmentChoice2, EquipmentChoice3))
                    .Select(purchase => $"Spec-Ops: {purchase}")
            ];
}
