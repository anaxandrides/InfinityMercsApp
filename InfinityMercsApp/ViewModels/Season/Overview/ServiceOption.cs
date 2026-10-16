using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Season.Services;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>A service as offered for the selected trooper: what it costs and does, and why it can't be bought.</summary>
public sealed partial class ServiceOption(SeasonService service) : ObservableObject
{
    public SeasonService Service { get; } = service;

    public string Name => Service.Name();

    public string CostText => $"{Service.Cost()} CR";

    public string Description => Service.Description();

    public bool ChoosesInjury => Service == SeasonService.AdvancedTherapy;

    public bool ChoosesPerks => Service == SeasonService.ForcedRetraining;

    /// <summary>Why the trooper can't have this service; empty when it can.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAvailable))]
    private string _unavailableReason = string.Empty;

    public bool IsAvailable => UnavailableReason.Length == 0;
}
