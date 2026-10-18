using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Services.CompanySelection.Captain;

public sealed class CaptainUpgradePopupContext
{
    public CaptainUnitPopupInfo Unit { get; init; } = new();
    public int OptionFactionId { get; init; }
    public string OptionFactionName { get; init; } = string.Empty;
    public List<string> WeaponOptions { get; init; } = [];
    public List<string> SkillOptions { get; init; } = [];
    public List<string> EquipmentOptions { get; init; } = [];
    public string PopupTitle { get; init; } = "Captain Configuration";
    public string ConfirmButtonText { get; init; } = "FOUND COMPANY";
    public string DefaultUnitCustomName { get; init; } = "Captain";
    public int? BaseExperienceOverride { get; init; }

    /// <summary>The Spec-Ops chart for a Spec Ops captain (and a TAG Company's Spec Ops); otherwise the Mercenaries chart.</summary>
    public CaptainStatChart StatChart { get; init; } = CaptainStatChart.Mercenaries;

    /// <summary>Stats the popup offers no upgrade for (a TAG's ARM, BTS and STR); <see cref="Stat.Vita"/> stands for the vitality picker.</summary>
    public IReadOnlyList<Stat> LockedStats { get; init; } = [];
}
