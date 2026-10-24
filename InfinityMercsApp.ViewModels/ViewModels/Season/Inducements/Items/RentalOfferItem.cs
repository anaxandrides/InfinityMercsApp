using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inducements;

namespace InfinityMercsApp.ViewModels.Season.Inducements.Items;

/// <summary>A Rented Equipment row, greyed out when the inducements left or its AVA can't take another.</summary>
public sealed partial class RentalOfferItem(RentalOffer offer) : ObservableObject
{
    public RentalOffer Offer { get; } = offer;

    public string Name => Offer.Name;

    /// <summary>"Primary · AVA 2".</summary>
    public string Detail => $"{Offer.Slot.Key()} · AVA {Offer.AvaText}";

    public string CostText => $"{Offer.Cost}";

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }
}
