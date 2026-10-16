using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Dialogs;

/// <summary>Asks the player to configure a captain (or a TAG's Spec Ops upgrades).</summary>
public interface ICaptainConfigurationDialog
{
    /// <returns>The configured upgrades, or null when the player backed out.</returns>
    Task<SavedImprovedCaptainStats?> ShowAsync(CaptainUpgradePopupContext context);
}
