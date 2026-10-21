namespace InfinityMercsApp.Domain.Season.RecruitRolls;

/// <summary>
/// A copy of armour that rolls on a recruit table when bought (Chemtank, the Irmandinho Wetsuit) keeps its result,
/// which goes with it to whoever wears it. The copy is named with its result: "Chemtank [Regeneration]".
/// </summary>
public sealed record RolledArmor(string ArmorName, string Result)
{
    public string Name => $"{ArmorName} [{Result}]";

    /// <summary>A rolled copy's armour and result; null for a name with no result in square brackets.</summary>
    public static RolledArmor? Parse(string name)
    {
        var trimmed = name.Trim();
        var open = trimmed.LastIndexOf(" [", StringComparison.Ordinal);
        return open > 0 && trimmed.EndsWith(']')
            ? new RolledArmor(trimmed[..open].Trim(), trimmed[(open + 2)..^1].Trim())
            : null;
    }
}
