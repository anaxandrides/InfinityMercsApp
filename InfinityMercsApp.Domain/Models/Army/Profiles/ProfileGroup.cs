namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// A group of related profiles and the options that field them (the API's <c>profileGroups[]</c>).
/// </summary>
public sealed class ProfileGroup
{
    public int? Id { get; init; }

    public string? Isc { get; init; }

    public int? Category { get; init; }

    public IReadOnlyList<UnitProfile> Profiles { get; set; } = [];

    public IReadOnlyList<ProfileOption> Options { get; set; } = [];

    /// <summary>
    /// Peripheral entries declared on the group's own profiles (a controller's peripherals).
    /// </summary>
    public IReadOnlyList<ProfileEntry> ControllerPeripheralEntries =>
        Profiles.SelectMany(profile => profile.Peripheral).ToList();
}
