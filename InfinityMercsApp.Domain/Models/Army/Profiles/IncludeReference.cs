namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// A reference from one option to another option whose entries it also grants.
/// </summary>
public sealed record IncludeReference(int GroupId, int OptionId);
