using InfinityMercsApp.Services.CompanySelection.Captain;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.Services.CompanySelection.SpecOps;

/// <summary>The TAG company's Spec-Ops rules: which faction they come from and which options a TAG can't take.</summary>
public static class TagSpecOps
{
    private static readonly IReadOnlyList<string> DisallowedOptions =
    [
        "Forward Deployment",
        "Strategic Deployment",
        "Infiltration",
        "Impersonation",
        "Parachutist",
        "Combat Drop",
        "Engineer"
    ];

    /// <summary>
    /// The faction a TAG's Spec-Ops come from: the sectorial chosen beside the TAG company, else the trooper's or
    /// the first source faction that isn't the TAG company itself.
    /// </summary>
    public static int SourceFactionId(CaptainFactions factions)
    {
        if (factions.PreferredOtherFactionId is int preferred && preferred > 0 && preferred != TagGen.TagCompanyFactionId)
        {
            return preferred;
        }

        var resolved = CaptainSetup.SourceFactionId(factions);
        if (resolved > 0 && resolved != TagGen.TagCompanyFactionId)
        {
            return resolved;
        }

        return factions.FirstSourceFactionId is int first && first > 0 && first != TagGen.TagCompanyFactionId
            ? first
            : resolved;
    }

    /// <summary>The vanilla faction a reinforcement faction (x99) belongs to (x01), e.g. 199 → 101; null for any other.</summary>
    public static int? VanillaOfReinforcement(int factionId)
    {
        if (factionId <= 0 || factionId % 100 != 99)
        {
            return null;
        }

        var vanillaFactionId = (factionId / 100) * 100 + 1;
        return vanillaFactionId > 0 ? vanillaFactionId : null;
    }

    /// <summary>The options without those a TAG can't take.</summary>
    public static CaptainUpgradeOptionSet WithoutDisallowed(CaptainUpgradeOptionSet source) => new()
    {
        Weapons = source.Weapons.Where(option => !IsDisallowed(option)).ToList(),
        Skills = source.Skills.Where(option => !IsDisallowed(option)).ToList(),
        Equipment = source.Equipment.Where(option => !IsDisallowed(option)).ToList()
    };

    private static bool IsDisallowed(string optionLabel) =>
        !string.IsNullOrWhiteSpace(optionLabel) &&
        DisallowedOptions.Any(disallowed => optionLabel.Contains(disallowed, StringComparison.OrdinalIgnoreCase));
}
