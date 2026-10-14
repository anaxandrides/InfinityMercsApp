namespace InfinityMercsApp.Views.Common.Captain;

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
