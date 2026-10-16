using System.Globalization;
using InfinityMercsApp.Domain.Models.Metadata.Ranges;

namespace InfinityMercsApp.ViewModels.Components;

/// <summary>What a weapon's range bar shows: its bands with their modifiers, and the distance at each band edge.</summary>
public sealed record RangeBar(IReadOnlyList<RangeBarBand> Bands, IReadOnlyList<RangeBarMark> Marks)
{
    public static RangeBar Empty { get; } = new([], []);

    /// <summary>
    /// The bar for <paramref name="distance"/>. With an X Visor a negative modifier improves by 3, to no
    /// better than 0. Distances show in whole inches (2.5 cm each, rounded) or in centimetres.
    /// </summary>
    public static RangeBar From(WeaponDistance? distance, bool xVisor, bool inInches)
    {
        var bands = distance?.GetOrderedBands() ?? [];
        double total = bands.Count == 0 ? 0 : bands[^1].EndCm;
        if (total <= 0)
        {
            return Empty;
        }

        var barBands = bands
            .Select(band =>
            {
                var modifier = xVisor ? WithXVisor(band.Mod) : band.Mod;
                return new RangeBarBand(band.StartCm / total, band.EndCm / total, modifier, ToneOf(modifier));
            })
            .ToList();
        List<RangeBarMark> marks = [new RangeBarMark(0, DistanceLabel(0, inInches))];
        marks.AddRange(bands.Select(band => new RangeBarMark(band.EndCm / total, DistanceLabel(band.EndCm, inInches))));
        return new RangeBar(barBands, marks);
    }

    /// <summary>An X Visor's modifier: a penalty improves by 3, to no better than 0; anything else is unchanged.</summary>
    public static string WithXVisor(string modifier) =>
        int.TryParse(modifier, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value < 0
            ? Math.Min(0, value + 3).ToString(CultureInfo.InvariantCulture)
            : modifier;

    public static RangeBandTone ToneOf(string modifier) =>
        !int.TryParse(modifier, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? RangeBandTone.Neutral
            : value switch
            {
                > 0 => RangeBandTone.Bonus,
                0 => RangeBandTone.Neutral,
                >= -3 => RangeBandTone.Penalty,
                _ => RangeBandTone.Severe
            };

    private static string DistanceLabel(int cm, bool inInches) =>
        inInches
            ? $"{(int)Math.Round(cm / 2.5, MidpointRounding.AwayFromZero)}\""
            : $"{cm}cm";
}
