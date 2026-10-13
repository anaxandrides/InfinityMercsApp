using System.Net;
using System.Text.Json;
using Markdig;

namespace InfinityMercsApp.Services;

/// <summary>A row of the rules index: a section header or a rule with its markdown file.</summary>
public sealed record GlossaryEntry(string Title, string MarkdownFile = "", bool HasDelta = false, bool IsHeader = false)
{
    public bool ShowDeltaIcon => !IsHeader && HasDelta;
}

/// <summary>Reads the MERCS glossary index and renders its markdown entries as HTML pages.</summary>
public static class MercsGlossary
{
    public const string IndexFile = "mercs_glossary.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    /// <summary>The markdown file's path inside the app package.</summary>
    public static string MarkdownPath(GlossaryEntry entry) => $"glossary/{entry.MarkdownFile}";

    /// <summary>
    /// The index rows in order. Sections give a header then their entries; a flat entry list starts a
    /// new header whenever an entry's section header changes. Blank titles are skipped.
    /// </summary>
    public static IReadOnlyList<GlossaryEntry> ParseIndex(string json)
    {
        var payload = JsonSerializer.Deserialize<Payload>(json, JsonOptions);
        var items = new List<GlossaryEntry>();

        if (payload?.Sections.Count > 0)
        {
            foreach (var section in payload.Sections)
            {
                AddHeader(section.HeaderOrTitle);
                section.Entries.ForEach(AddEntry);
            }

            return items;
        }

        string? lastHeader = null;
        foreach (var entry in payload?.Entries ?? [])
        {
            var header = entry.SectionHeaderOrHeader;
            if (!string.IsNullOrWhiteSpace(header) && !string.Equals(header, lastHeader, StringComparison.Ordinal))
            {
                AddHeader(header);
                lastHeader = header;
            }

            AddEntry(entry);
        }

        return items;

        void AddHeader(string? header)
        {
            if (!string.IsNullOrWhiteSpace(header))
            {
                items.Add(new GlossaryEntry(header.Trim(), IsHeader: true));
            }
        }

        void AddEntry(PayloadEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.Title))
            {
                items.Add(new GlossaryEntry(entry.Title.Trim(), entry.MarkdownFile?.Trim() ?? string.Empty, entry.HasDelta));
            }
        }
    }

    /// <summary>A rule's markdown as a styled HTML page.</summary>
    public static string MarkdownPage(string markdown) => WrapHtmlDocument(Markdown.ToHtml(markdown, Pipeline));

    /// <summary>Plain text (a message, not markdown) as a styled HTML page.</summary>
    public static string TextPage(string text)
    {
        var encoded = WebUtility.HtmlEncode(text ?? string.Empty);
        return WrapHtmlDocument($"<p>{encoded.Replace("\n", "<br />")}</p>");
    }

    private static string WrapHtmlDocument(string htmlBody) => $$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <style>
        html, body {
            margin: 0;
            padding: 0;
            background: #0E1116;
            color: #E6EBF2;
            font-family: Segoe UI, Arial, sans-serif;
            font-size: 18px;
            line-height: 1.4;
        }
        body {
            padding: 6px;
        }
        h1, h2, h3, h4, h5, h6 {
            color: #E6EBF2;
            margin-top: 0.6em;
            margin-bottom: 0.3em;
        }
        p, ul, ol, pre, blockquote {
            margin-top: 0.25em;
            margin-bottom: 0.7em;
        }
        code, pre {
            font-family: Consolas, "Courier New", monospace;
            background: #161B22;
            color: #B5C0CE;
        }
        pre {
            padding: 0.4em;
            border-radius: 4px;
            overflow-x: auto;
        }
        a {
            color: #34D399;
        }
    </style>
</head>
<body>
{{htmlBody}}
</body>
</html>
""";

    private sealed class Payload
    {
        public List<PayloadEntry> Entries { get; init; } = [];
        public List<PayloadSection> Sections { get; init; } = [];
    }

    private sealed class PayloadSection
    {
        public string Header { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public List<PayloadEntry> Entries { get; init; } = [];

        public string HeaderOrTitle => string.IsNullOrWhiteSpace(Header) ? Title : Header;
    }

    private sealed class PayloadEntry
    {
        public string Title { get; init; } = string.Empty;
        public string MarkdownFile { get; init; } = string.Empty;
        public bool HasDelta { get; init; }
        public string SectionHeader { get; init; } = string.Empty;
        public string Header { get; init; } = string.Empty;

        public string SectionHeaderOrHeader => string.IsNullOrWhiteSpace(SectionHeader) ? Header : SectionHeader;
    }
}
