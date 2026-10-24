using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Packaging;

namespace InfinityMercsApp.Presentation.Company.Selection;

/// <summary>
/// Which logo a company selection screen shows for a unit or a faction: a downloaded one when it exists, else
/// a packaged one. A generated faction has only its own packaged logo.
/// </summary>
public sealed class UnitLogoSources(FactionLogoCacheService? logos, IAppPackageFiles packageFiles, GeneratedFactionLogo? generated)
{
    /// <summary>A faction's downloaded logo, used when a unit has none of its own.</summary>
    public string? CachedFactionLogoPath(int factionId) =>
        factionId == generated?.FactionId ? null : logos?.GetCachedLogoPath(factionId);

    /// <summary>A faction's packaged logo, used when a unit has none of its own.</summary>
    public string? PackagedFactionLogoPath(int factionId) =>
        factionId == generated?.FactionId ? generated.LogoPath : logos?.GetPackagedFactionLogoPath(factionId);

    /// <summary>A unit's packaged logo, by the logo service's path or the packaged folder's own.</summary>
    public string PackagedUnitLogoPath(int factionId, int unitId) =>
        logos?.GetPackagedUnitLogoPath(factionId, unitId) ?? $"SVGCache/units/{factionId}-{unitId}.svg";

    /// <summary>
    /// The first logo that exists for a unit: its own or its faction's downloaded logo, under each of the request's
    /// factions in turn, else a packaged one, else the unit's packaged path.
    /// </summary>
    public async Task<string?> SourceAsync(UnitLogoRequest unit)
    {
        if (CachedCandidates(unit).FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path)) is { } cachedPath)
        {
            return cachedPath;
        }

        foreach (var candidate in PackagedCandidates(unit).OfType<string>().Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var normalized = candidate.Replace('\\', '/').TrimStart('/');
            if (await packageFiles.ExistsAsync(normalized))
            {
                return normalized;
            }
        }

        return unit.PackagedLogoPath;
    }

    // The unit's own downloaded logo, then its logo under each faction, then its faction's logo.
    private IEnumerable<string?> CachedCandidates(UnitLogoRequest unit)
    {
        yield return unit.CachedLogoPath;
        if (logos is null)
        {
            yield break;
        }

        foreach (var factionId in unit.FactionIds)
        {
            yield return logos.GetCachedUnitLogoPath(factionId, unit.UnitId);
        }

        yield return CachedFactionLogoPath(unit.SourceFactionId);
    }

    // The packaged logos in the same order.
    private IEnumerable<string?> PackagedCandidates(UnitLogoRequest unit)
    {
        yield return unit.PackagedLogoPath;
        foreach (var factionId in unit.FactionIds)
        {
            yield return PackagedUnitLogoPath(factionId, unit.UnitId);
        }

        yield return logos is null ? $"SVGCache/factions/{unit.SourceFactionId}.svg" : PackagedFactionLogoPath(unit.SourceFactionId);
    }
}
