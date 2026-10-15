using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.SpecOps;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Captain;

public sealed class CaptainWorkflowDialogTests
{
    private readonly ICaptainConfigurationDialog _dialog = Substitute.For<ICaptainConfigurationDialog>();
    private readonly ISpecOpsProvider _specOps = Substitute.For<ISpecOpsProvider>();

    [Fact]
    public async Task TheCaptainPopupIsShownThroughTheDialog()
    {
        var chosen = new SavedImprovedCaptainStats { IsEnabled = true, CaptainName = "Kestrel" };
        _dialog.ShowAsync(Arg.Any<CaptainUpgradePopupContext>()).Returns(chosen);

        var result = await CompanyCaptainWorkflowService.ShowCaptainConfigurationAsync(new CompanyCaptainWorkflowRequest
        {
            CaptainDialog = _dialog,
            FallbackSourceFactionId = Army101Data.PanOceania,
            FirstSourceFactionId = Army101Data.PanOceania,
            UnitName = "Fusilier",
            UnitCost = 10,
            UnitStatline = "CC 13 | BS 12",
            UnitRangedWeapons = "Combi Rifle",
            UnitCcWeapons = "Pistol",
            UnitSkills = "Lieutenant",
            UnitEquipment = "-",
            TryGetParentFactionId = _ => null,
            TryGetFactionName = _ => "PanOceania",
            TryGetMetadataFactionName = _ => null,
            ArmyDataService = Army101Data.Create(),
            SpecOpsProvider = _specOps,
            ShowUnitsInInches = true
        });

        Assert.Same(chosen, result);
        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.Unit.Name == "Fusilier" && context.Unit.Cost == 10));
    }

    [Fact]
    public async Task ATagGetsItsOwnSpecOpsPopup()
    {
        await CompanySpecOpsWorkflowService.ShowTagSpecOpsConfigurationAsync(new CompanySpecOpsWorkflowRequest
        {
            CaptainDialog = _dialog,
            FallbackSourceFactionId = Army101Data.PanOceania,
            FirstSourceFactionId = Army101Data.PanOceania,
            UnitName = "Squalo",
            UnitCost = 60,
            UnitStatline = "-",
            UnitRangedWeapons = "-",
            UnitCcWeapons = "-",
            UnitSkills = "-",
            UnitEquipment = "-",
            TryGetParentFactionId = _ => null,
            TryGetFactionName = _ => null,
            TryGetMetadataFactionName = _ => null,
            ArmyDataService = Army101Data.Create(),
            SpecOpsProvider = _specOps,
            ShowUnitsInInches = true,
            IsLieutenant = false,
            BaseExperience = 20
        });

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.PopupTitle == "TAG Spec Ops" &&
            context.ConfirmButtonText == "Finalize Tag" &&
            context.DefaultUnitCustomName == "TAG" &&
            context.BaseExperienceOverride == 20));
    }
}
