using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.ViewModels.Season.Inducements.Items;

/// <summary>Gear rented for the contract, and who carries it; it can be returned again.</summary>
/// <param name="Text">"AP Marksman Rifle: Bolt (Primary)".</param>
public sealed record RentalRow(RentedGear Rental, string Text);
