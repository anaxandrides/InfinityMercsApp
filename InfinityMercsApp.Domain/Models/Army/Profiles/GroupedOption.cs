namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>An option together with the profile group it belongs to.</summary>
public sealed record GroupedOption(ProfileGroup Group, ProfileOption Option);
