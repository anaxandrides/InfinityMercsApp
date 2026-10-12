using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using Resume = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.ViewModels;

public partial class ViewerViewModel
{
    private static List<(string Text, string? Url)> BuildLinkedLines(
        IEnumerable<(int Id, string Name)> entries,
        IReadOnlyDictionary<int, string> links)
    {
        var result = new List<(string Text, string? Url)>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var url = links.TryGetValue(entry.Id, out var resolvedUrl) ? resolvedUrl : null;
            var existingIndex = result.FindIndex(x => string.Equals(x.Text, entry.Name, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                if (string.IsNullOrWhiteSpace(result[existingIndex].Url) && !string.IsNullOrWhiteSpace(url))
                {
                    result[existingIndex] = (result[existingIndex].Text, url);
                }

                continue;
            }

            result.Add((entry.Name, url));
        }

        return result;
    }

    private static FormattedString BuildLinkedFormattedString(IEnumerable<(string Text, string? Url)> lines, Color textColor)
    {
        var formatted = new FormattedString();
        var list = lines.ToList();
        if (list.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-", TextColor = textColor });
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var line = list[i];
            AppendWithLieutenantHighlight(formatted, line.Text, textColor, line.Url);
            if (i < list.Count - 1)
            {
                formatted.Spans.Add(new Span { Text = Environment.NewLine, TextColor = textColor });
            }
        }

        return formatted;
    }

    private static void AppendWithLieutenantHighlight(FormattedString formatted, string text, Color defaultColor, string? link = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var matches = Regex.Matches(text, "(lieutenant)", RegexOptions.IgnoreCase);
        if (matches.Count == 0)
        {
            var span = new Span
            {
                Text = text,
                TextColor = defaultColor
            };
            AttachLinkGesture(span, link);
            formatted.Spans.Add(span);
            return;
        }

        var currentIndex = 0;
        foreach (Match match in matches)
        {
            if (match.Index > currentIndex)
            {
                var prefixSpan = new Span
                {
                    Text = text.Substring(currentIndex, match.Index - currentIndex),
                    TextColor = defaultColor
                };
                AttachLinkGesture(prefixSpan, link);
                formatted.Spans.Add(prefixSpan);
            }

            var highlightedSpan = new Span
            {
                Text = text.Substring(match.Index, match.Length),
                TextColor = Color.FromArgb("#C084FC")
            };
            AttachLinkGesture(highlightedSpan, link);
            formatted.Spans.Add(highlightedSpan);

            currentIndex = match.Index + match.Length;
        }

        if (currentIndex < text.Length)
        {
            var suffixSpan = new Span
            {
                Text = text[currentIndex..],
                TextColor = defaultColor
            };
            AttachLinkGesture(suffixSpan, link);
            formatted.Spans.Add(suffixSpan);
        }
    }

    private static void AttachLinkGesture(Span span, string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return;
        }

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await OpenLinkAsync(link);
        span.GestureRecognizers.Add(tap);
    }

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
