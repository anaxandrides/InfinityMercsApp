using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Services.CompanySelection.Models.Saved;

/// <summary>
/// Names turned into saved codes: <see cref="Codes"/> for every name found in the army filters, and
/// <see cref="CustomNames"/> for names (or bracketed extras) that had no code and are kept as text.
/// </summary>
public sealed record CodedNames(List<CompanySavedCodeRef> Codes, List<string> CustomNames);
