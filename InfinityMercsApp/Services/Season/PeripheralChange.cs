namespace InfinityMercsApp.Services.Season;

/// <summary>
/// What a gear change does to Peripheral (Controller) augments: the troopers that lose one, and the trooper that
/// gains one (with its name, for the pick).
/// </summary>
public sealed record PeripheralChange(IReadOnlyList<int> LosingControl, int? GainingControl, string GainingName)
{
    public bool IsEmpty => LosingControl.Count == 0 && GainingControl is null;
}
