using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Season.Inducements;

namespace InfinityMercsApp.ViewModels.Season.Inducements.Items;

/// <summary>A Troops for Hire row, greyed out when the inducements left or the room left can't take it.</summary>
public sealed partial class HireOfferItem(HiredTroopOffer offer) : ObservableObject
{
    public HiredTroopOffer Offer { get; } = offer;

    public string Name => Offer.Display;

    public string CostText => $"{Offer.Cost}";

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }
}
