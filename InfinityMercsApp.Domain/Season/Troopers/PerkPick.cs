namespace InfinityMercsApp.Domain.Season.Troopers;

/// <summary>A perk a trooper can take: the node, its text, and where it sits in its list.</summary>
public sealed record PerkPick(string NodeId, string Name, string ListId, string ListName, int Track, int Tier);
