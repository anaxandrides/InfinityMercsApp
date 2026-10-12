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
    private static string BuildNamedSummary(string label, IEnumerable<string> values)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return list.Count == 0
            ? $"{label}: -"
            : $"{label}: {string.Join(", ", list)}";
    }

    private static FormattedString BuildNamedSummaryFormatted(
        string label,
        IEnumerable<string> values,
        IReadOnlyDictionary<int, string>? equipLookup,
        IReadOnlyDictionary<int, string>? links,
        Color? textColor)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var formatted = new FormattedString();
        var labelSpan = new Span { Text = $"{label}: " };
        if (textColor is not null)
        {
            labelSpan.TextColor = textColor;
        }

        formatted.Spans.Add(labelSpan);

        if (list.Count == 0)
        {
            var emptySpan = new Span { Text = "-" };
            if (textColor is not null)
            {
                emptySpan.TextColor = textColor;
            }

            formatted.Spans.Add(emptySpan);
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var value = list[i];
            var valueColor = textColor ?? Colors.White;
            var link = TryResolveLinkForDisplayName(value, equipLookup, links);
            AppendWithLieutenantHighlight(formatted, value, valueColor, link);
            if (i < list.Count - 1)
            {
                var separatorSpan = new Span { Text = ", " };
                if (textColor is not null)
                {
                    separatorSpan.TextColor = textColor;
                }

                formatted.Spans.Add(separatorSpan);
            }
        }

        return formatted;
    }

    private static string? TryResolveLinkForDisplayName(
        string displayName,
        IReadOnlyDictionary<int, string>? nameLookup,
        IReadOnlyDictionary<int, string>? links)
    {
        if (string.IsNullOrWhiteSpace(displayName) || nameLookup is null || links is null)
        {
            return null;
        }

        foreach (var pair in nameLookup)
        {
            if (!links.ContainsKey(pair.Key))
            {
                continue;
            }

            if (string.Equals(pair.Value, displayName, StringComparison.OrdinalIgnoreCase))
            {
                return links[pair.Key];
            }
        }

        var trimmed = displayName;
        var parenIndex = displayName.IndexOf(" (", StringComparison.Ordinal);
        if (parenIndex > 0)
        {
            trimmed = displayName[..parenIndex];
        }

        foreach (var pair in nameLookup)
        {
            if (!links.ContainsKey(pair.Key))
            {
                continue;
            }

            if (string.Equals(pair.Value, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return links[pair.Key];
            }
        }

        return null;
    }

    private static string? TryResolveFirstLinkByPredicate(
        IReadOnlyDictionary<int, string> nameLookup,
        IReadOnlyDictionary<int, string> links,
        Func<string, bool> predicate)
    {
        foreach (var pair in nameLookup)
        {
            if (!links.TryGetValue(pair.Key, out var url))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (predicate(pair.Value))
            {
                return url;
            }
        }

        return null;
    }

    private static bool IsMeleeWeaponName(string weaponName) =>
        Regex.IsMatch(
            weaponName,
            @"\bccw\b|\bda ccw\b|\bap ccw\b|\bknife\b|\bsword\b|\bmonofilament\b|\bviral ccw\b|\bpistols?\b|\bclose combat weapon\b|\bcc\s*weapon\b|\bc\.?\s*c\.?\s*weapon\b|\bpara\s*cc\s*weapon\b",
            RegexOptions.IgnoreCase);

    private static string JoinOrDash(IEnumerable<string> values)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return list.Count == 0 ? "-" : string.Join(Environment.NewLine, list);
    }

    private static FormattedString BuildNameFormatted(string name)
    {
        var formatted = new FormattedString();
        if (string.IsNullOrWhiteSpace(name))
        {
            formatted.Spans.Add(new Span { Text = string.Empty });
            return formatted;
        }

        const string token = "Lieutenant";
        var start = 0;
        while (true)
        {
            var index = name.IndexOf(token, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                if (start < name.Length)
                {
                    formatted.Spans.Add(new Span { Text = name[start..] });
                }

                break;
            }

            if (index > start)
            {
                formatted.Spans.Add(new Span { Text = name[start..index] });
            }

            formatted.Spans.Add(new Span
            {
                Text = name.Substring(index, token.Length),
                TextColor = Color.FromArgb("#C084FC")
            });
            start = index + token.Length;
        }

        return formatted;
    }

    private static List<(int Id, string Name)> BuildConfigurationSkillEntries(IEnumerable<(int Id, string Name)> rawEntries)
    {
        var normalized = new List<(int Id, string Name)>();
        foreach (var entry in rawEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var skillName = entry.Name.Trim();
            if (!IsCommonSpecOpsSkill(skillName))
            {
                normalized.Add((entry.Id, skillName));
                continue;
            }

            var lieutenantDetail = ExtractLieutenantSkillDetail(skillName);
            if (!string.IsNullOrWhiteSpace(lieutenantDetail))
            {
                normalized.Add((0, lieutenantDetail));
            }
        }

        return normalized
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsCommonSpecOpsSkill(string? skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return false;
        }

        return skillName.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) ||
               skillName.Contains("infinity spec-ops", StringComparison.OrdinalIgnoreCase) ||
               skillName.Contains("infinity spec ops", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractLieutenantSkillDetail(string skillName)
    {
        if (!skillName.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var detail = Regex.Replace(skillName, "lieutenant", string.Empty, RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(detail))
        {
            return null;
        }

        detail = detail.Trim('(', ')', '[', ']', '-', ':', ',', ';', ' ');
        return string.IsNullOrWhiteSpace(detail) ? null : detail;
    }

}
