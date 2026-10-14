using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Views.Common.Captain;

public sealed class CaptainUpgradeOptionSet
{
    public static CaptainUpgradeOptionSet Empty { get; } = new();
    public List<string> Weapons { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<string> Equipment { get; init; } = [];
    public bool IsEmpty => Weapons.Count == 0 && Skills.Count == 0 && Equipment.Count == 0;
}

public sealed class CaptainUnitPopupInfo
{
    public string Name { get; init; } = string.Empty;
    public int Cost { get; init; }
    public string Statline { get; init; } = "-";
    public string RangedWeapons { get; init; } = "-";
    public string CcWeapons { get; init; } = "-";
    public string Skills { get; init; } = "-";
    public string Equipment { get; init; } = "-";
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }
}

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
}

/// <summary>A captain stat upgrade level as a picker item.</summary>
public sealed record StatPickerOption(Stat Stat, int Tier, int Bonus, int Cost)
{
    public string Label => $"{Stat.Key()} +{Bonus} | {Cost}xp";

    public static StatPickerOption From(InfinityMercsApp.Domain.Captain.CaptainStatOption option) =>
        new(option.Stat, option.Tier, option.Bonus, option.Cost);
}

