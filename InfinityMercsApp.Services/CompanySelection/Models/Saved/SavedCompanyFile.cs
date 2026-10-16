namespace InfinityMercsApp.Services.CompanySelection.Models.Saved;

/// <summary>
/// Concrete saved-company DTO shared by every company type. All company pages persist the same
/// JSON shape, and all loaders (season, viewer, load, play/game mode) deserialize into this type,
/// so a single shared set replaces the previously duplicated per-page <c>Saved*</c> definitions.
/// </summary>
public sealed class SavedCompanyFile : CompanySavedCompanyFileBase<SavedCompanyFaction, SavedCompanyEntry>
{
}
