using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.CompanySelection.SpecOps;

/// <summary>A SpecBot a Spec-Ops can bring as its peripheral: how it reads in a list, its cost and its stat block.</summary>
public sealed record SpecBotOption(string Label, int Cost, PeripheralMercsCompanyStats Stats);
