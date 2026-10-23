namespace InfinityMercsApp.Api.ArmyData.Logos;

/// <summary>A faction's or unit's id in the army data, and the address of its logo.</summary>
internal sealed record IdentifiedLogo(int Id, string Url);
