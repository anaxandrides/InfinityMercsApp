namespace InfinityMercsApp.Domain.Models.Army.Stats;

/// <summary>The unit type codes the rules single out.</summary>
public static class UnitTypeCodes
{
    public const string LightInfantry = "LI";
    public const string MediumInfantry = "MI";
    public const string HeavyInfantry = "HI";
    public const string Tag = "TAG";
    public const string Remote = "REM";
    public const string Peripheral = "PERIPHERAL";

    /// <summary>The troop type a list subtitle starts with ("LI - Garrison Troops" → "LI"); empty when there is none.</summary>
    public static string FromSubtitle(string? subtitle) =>
        subtitle?.Split([' ', '-', '\u2013', '\u2014'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim().ToUpperInvariant()
        ?? string.Empty;

    public static bool IsTag(string? unitTypeCode) => Is(unitTypeCode, Tag);

    public static bool IsRemote(string? unitTypeCode) => Is(unitTypeCode, Remote);

    public static bool IsHeavyInfantry(string? unitTypeCode) => Is(unitTypeCode, HeavyInfantry);

    /// <summary>TAGs, remotes and peripherals have structure (STR) instead of wounds (VITA).</summary>
    public static bool HasStructure(string? unitTypeCode) =>
        Is(unitTypeCode, Tag) || Is(unitTypeCode, Remote) || Is(unitTypeCode, Peripheral);

    public static bool Is(string? unitTypeCode, string code) =>
        string.Equals(unitTypeCode?.Trim(), code, StringComparison.OrdinalIgnoreCase);
}
