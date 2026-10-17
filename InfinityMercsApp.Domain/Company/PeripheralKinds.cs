using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Company;

/// <summary>
/// Mercenaries reworks peripherals: a Servant or Ancillary peripheral stays equipment of the trooper it belongs to,
/// but any other kind is a trooper on the roster in its own right. It deploys, gains XP and equipment, and is
/// Irregular; its controller gains FT Master (own peripheral).
/// </summary>
public static partial class PeripheralKinds
{
    public const string ControllerSkill = "FT Master (own peripheral)";
    public const string Irregular = "Irregular";
    public const string Regular = "Regular";

    /// <summary>Whether a peripheral with these skills and characteristics is a Servant or Ancillary one.</summary>
    public static bool IsServant(IEnumerable<string> skillsAndCharacteristics) =>
        skillsAndCharacteristics.Any(line => ServantKind().IsMatch(line.Replace(' ', ' ')));

    /// <summary>A peripheral trooper's characteristics: Irregular in place of Regular.</summary>
    public static IReadOnlyList<string> AsTrooper(IEnumerable<string> characteristics) =>
        [
            .. characteristics.Where(characteristic =>
                !string.Equals(characteristic.Trim(), Regular, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(characteristic.Trim(), Irregular, StringComparison.OrdinalIgnoreCase)),
            Irregular
        ];

    [GeneratedRegex(@"^\s*Peripheral\s*\([^)]*\b(Servant|Ancillary)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ServantKind();
}
