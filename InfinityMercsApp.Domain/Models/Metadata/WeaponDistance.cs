namespace InfinityMercsApp.Domain.Models.Metadata;

/// <summary>One range band of a weapon: its upper bound (in cm) and the modifier that applies within it.</summary>
public class WeaponDistanceBand
{
    public int Max { get; set; }

    public string Mod { get; set; } = "0";
}

/// <summary>A weapon's range bands, as the API's <c>distance</c> object describes them.</summary>
public class WeaponDistance
{
    public const string ShortBand = "short";
    public const string MediumBand = "med";
    public const string LongBand = "long";
    public const string MaximumBand = "max";

    /// <summary>Every band the API sends, in payload order, including rare ones such as <c>xlong</c>.</summary>
    public IReadOnlyList<(string Name, WeaponDistanceBand Band)> Bands { get; init; } = [];

    public WeaponDistanceBand? Short => Find(ShortBand);

    public WeaponDistanceBand? Med => Find(MediumBand);

    public WeaponDistanceBand? Long => Find(LongBand);

    public WeaponDistanceBand? Max => Find(MaximumBand);

    /// <summary>The short, medium, long and maximum bands that exist, each running from the previous band's end.</summary>
    public IReadOnlyList<(string Label, int RangeStart, int RangeEnd, string Mod)> GetOrderedBands()
    {
        var result = new List<(string, int, int, string)>();
        int previous = 0;

        void AddBand(string label, WeaponDistanceBand? band)
        {
            if (band is null) return;
            result.Add((label, previous, band.Max, band.Mod));
            previous = band.Max;
        }

        AddBand(ShortBand, Short);
        AddBand(MediumBand, Med);
        AddBand(LongBand, Long);
        AddBand(MaximumBand, Max);
        return result;
    }

    private WeaponDistanceBand? Find(string name)
    {
        // A repeated band name: the last one wins, as when the JSON object was deserialized.
        WeaponDistanceBand? found = null;
        foreach (var (bandName, band) in Bands)
        {
            if (string.Equals(bandName, name, StringComparison.Ordinal))
            {
                found = band;
            }
        }

        return found;
    }
}
