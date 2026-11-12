using InfinityMercsApp.Domain.SpecOps;

namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

/// <summary>An item a trooper can buy with one-off credits: how it reads in a list, its price, and what it adds.</summary>
public sealed record StartingGearOption(string Label, int Cost, TrooperUpgrade Upgrade);
