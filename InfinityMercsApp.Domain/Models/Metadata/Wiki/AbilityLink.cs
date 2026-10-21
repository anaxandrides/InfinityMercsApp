namespace InfinityMercsApp.Domain.Models.Metadata.Wiki;

/// <summary>One ability from a comma-separated list, with its wiki page when it names a known skill.</summary>
public sealed record AbilityLink(string Text, string? WikiUrl)
{
    public bool IsSkill => WikiUrl is not null;
}
