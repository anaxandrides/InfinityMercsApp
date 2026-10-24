using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Presentation.Text;

namespace InfinityMercsApp.Converters;

/// <summary>Turns a view model's <see cref="RichText"/> into a Label's formatted text; a run with a link opens it when tapped.</summary>
public sealed class RichTextConverter : IValueConverter
{
    // The spacer between list entries: a near-zero-height blank line, about 2px.
    private const double EntryGapFontSize = 2d;

    // One command opens every link; each span gives its link as the parameter.
    private static readonly AsyncRelayCommand<string> OpenLinkCommand = new(OpenLinkAsync);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var formatted = new FormattedString();
        if (value is not RichText text)
        {
            return formatted;
        }

        foreach (var run in text.Runs)
        {
            var span = new Span { Text = run.Text };
            if (run.Color is { } color)
            {
                span.TextColor = color;
            }

            if (run.IsEntryGap)
            {
                span.FontSize = EntryGapFontSize;
            }

            if (run.Link is { } link)
            {
                span.GestureRecognizers.Add(new TapGestureRecognizer { Command = OpenLinkCommand, CommandParameter = link });
            }

            formatted.Spans.Add(span);
        }

        return formatted;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static async Task OpenLinkAsync(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(link);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open link '{link}': {ex.Message}");
        }
    }
}
