namespace InfinityMercsApp.Domain.Models.Metadata;

/// <summary>
/// One range band of a weapon as the API names it (e.g. "short"): its upper bound in cm and the modifier
/// that applies within it.
/// </summary>
public class WeaponDistanceBand
{
    public string Name { get; set; } = string.Empty;

    public int Max { get; set; }

    public string Mod { get; set; } = "0";
}

/// <summary>A range band laid out on the range bar: where it starts and ends (in cm) and its modifier.</summary>
public sealed record RangeBand(string Label, int StartCm, int EndCm, string Mod);

/// <summary>A weapon's range bands, as the API's <c>distance</c> object describes them.</summary>
public class WeaponDistance
{
    public const string ShortBand = "short";
    public const string MediumBand = "med";
    public const string LongBand = "long";
    public const string MaximumBand = "max";

    /// <summary>Every band the API sends, in payload order, including rare ones such as <c>xlong</c>.</summary>
    public IReadOnlyList<WeaponDistanceBand> Bands { get; init; } = [];

    public WeaponDistanceBand? Short => Find(ShortBand);

    public WeaponDistanceBand? Med => Find(MediumBand);

    public WeaponDistanceBand? Long => Find(LongBand);

    public WeaponDistanceBand? Max => Find(MaximumBand);

    /// <summary>The short, medium, long and maximum bands that exist, each running from the previous band's end.</summary>
    public IReadOnlyList<RangeBand> GetOrderedBands()
    {
        var result = new List<RangeBand>();
        var previous = 0;
        IEnumerable<WeaponDistanceBand?> barBands = [Short, Med, Long, Max];
        foreach (var band in barBands.OfType<WeaponDistanceBand>())
        {
            result.Add(new RangeBand(band.Name, previous, band.Max, band.Mod));
            previous = band.Max;
        }

        return result;
    }

    // A repeated band name: the last one wins, as when the JSON object was deserialized.
    private WeaponDistanceBand? Find(string name) =>
        Bands.LastOrDefault(band => string.Equals(band.Name, name, StringComparison.Ordinal));
}
