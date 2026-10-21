using System.Globalization;
using InfinityMercsApp.Domain.Lieutenants;

namespace InfinityMercsApp.Converters;

/// <summary>Turns text runs into formatted text, with lieutenant mentions in <see cref="HighlightColor"/>.</summary>
public sealed class LieutenantRunsConverter : IValueConverter
{
    public Color TextColor { get; set; } = Colors.White;

    public Color HighlightColor { get; set; } = Color.FromArgb("#34D399");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var formatted = new FormattedString();
        if (value is not IEnumerable<TextRun> runs)
        {
            return formatted;
        }

        foreach (var run in runs)
        {
            formatted.Spans.Add(new Span { Text = run.Text, TextColor = run.IsLieutenant ? HighlightColor : TextColor });
        }

        return formatted;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
