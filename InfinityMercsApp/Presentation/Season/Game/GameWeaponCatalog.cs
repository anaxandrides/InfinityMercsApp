using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
namespace InfinityMercsApp.Presentation.Season.Game;

/// <summary>
/// Builds a trooper's weapon cards from the metadata: ranged weapons with a range band first, then the
/// rest, then any the metadata doesn't know; all CC weapons on one card; then Discover and Suppressive Fire.
/// </summary>
public sealed partial class GameWeaponCatalog(IMetadataProvider? metadata)
{
    public const string CcCardTitle = "CC Weapon";

    private Dictionary<int, string>? _ammoNames;

    public IReadOnlyList<GameWeaponCard> CardsFor(string? rangedWeapons, string? meleeWeapons, AttackModifiers modifiers)
    {
        var cards = new List<GameWeaponCard>();
        var ranged = new WeaponModeModifiers(modifiers.BsDamageReduction, modifiers.BsAmmoModifier, modifiers.BsBurstBonus);

        var rangedLines = Resolve(rangedWeapons);
        cards.AddRange(rangedLines
            .OrderBy(line => line.Weapons.Count == 0 ? 2 : line.Weapons.Any(w => w.Distance is not null) ? 0 : 1)
            .ThenBy(line => line.BaseName, StringComparer.OrdinalIgnoreCase)
            .Select(line => line.Weapons.Count == 0
                ? GameWeaponCard.Unmatched(line.Line)
                : Card(line.Line, line.Weapons, ranged, modifiers.HasXVisor)));

        var meleeLines = Resolve(meleeWeapons);
        var meleeWeaponModes = meleeLines
            .SelectMany(line => line.Weapons)
            .OrderBy(weapon => string.IsNullOrWhiteSpace(weapon.Mode) ? weapon.Name : weapon.Mode, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (meleeWeaponModes.Count > 0)
        {
            cards.Add(Card(CcCardTitle, meleeWeaponModes, new WeaponModeModifiers(modifiers.CcDamageReduction, null, 0), modifiers.HasXVisor));
        }

        cards.AddRange(meleeLines.Where(line => line.Weapons.Count == 0).Select(line => GameWeaponCard.Unmatched(line.Line)));

        // Discover rolls WIP, so nothing applies; Suppressive Fire keeps only the damage reduction.
        AddNamedCard(cards, "Discover", "Discover", WeaponModeModifiers.None, modifiers.HasXVisor);
        AddNamedCard(cards, WeaponModeModifiers.SuppressiveFireMode, "Suppressive Fire Mode Weapon", ranged with { AmmoModifier = null, BurstBonus = 0 }, modifiers.HasXVisor);
        return cards;
    }

    private void AddNamedCard(List<GameWeaponCard> cards, string title, string searchName, WeaponModeModifiers card, bool hasXVisor)
    {
        var weapons = Search(searchName);
        if (weapons.Count > 0)
        {
            cards.Add(Card(title, weapons, card, hasXVisor));
        }
    }

    private GameWeaponCard Card(string title, IReadOnlyList<Weapon> weapons, WeaponModeModifiers card, bool hasXVisor)
    {
        var modes = weapons
            .Select((weapon, index) =>
            {
                var modifiers = WeaponModeModifiers.For(weapon, weapons, title, card);
                return new GameWeaponMode(weapon, modifiers, modifiers.Ammo(AmmoName(weapon)), index == 0);
            })
            .ToList();
        return new GameWeaponCard(title, modes, hasXVisor);
    }

    private IReadOnlyList<WeaponLine> Resolve(string? weaponsText) =>
        Lines(weaponsText)
            .Select(line =>
            {
                var baseName = LeadingName().Match(line).Value.Trim();
                return new WeaponLine(line, baseName, Search(MetadataName(baseName)));
            })
            .ToList();

    // The exact name when the metadata has it, otherwise every partial match.
    private IReadOnlyList<Weapon> Search(string name)
    {
        var matches = metadata?.SearchWeaponsByName(name) ?? [];
        var exact = matches.Where(weapon => string.Equals(weapon.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        return exact.Count > 0 ? exact : matches;
    }

    // Army builder abbreviations that the metadata spells out.
    private static string MetadataName(string baseName) => baseName switch
    {
        "T2 CCW" => "T2 CC Weapon",
        _ => baseName
    };

    private string? AmmoName(Weapon weapon)
    {
        if (weapon.AmmunitionId is not int id)
        {
            return null;
        }

        _ammoNames ??= (metadata?.GetAmmunitions() ?? [])
            .GroupBy(ammo => ammo.Id)
            .ToDictionary(group => group.Key, group => group.First().Name);
        return _ammoNames.TryGetValue(id, out var name) ? name : null;
    }

    private static IEnumerable<string> Lines(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && line != "-");

    [GeneratedRegex(@"^[^(]+")]
    private static partial Regex LeadingName();

    private sealed record WeaponLine(string Line, string BaseName, IReadOnlyList<Weapon> Weapons);
}
