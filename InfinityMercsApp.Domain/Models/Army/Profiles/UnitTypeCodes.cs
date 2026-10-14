namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>The unit type codes the rules single out.</summary>
public static class UnitTypeCodes
{
    public const string Tag = "TAG";
    public const string Remote = "REM";
    public const string Peripheral = "PERIPHERAL";

    public static bool IsTag(string? unitTypeCode) => Is(unitTypeCode, Tag);

    /// <summary>TAGs, remotes and peripherals have structure (STR) instead of wounds (VITA).</summary>
    public static bool HasStructure(string? unitTypeCode) =>
        Is(unitTypeCode, Tag) || Is(unitTypeCode, Remote) || Is(unitTypeCode, Peripheral);

    private static bool Is(string? unitTypeCode, string code) =>
        string.Equals(unitTypeCode?.Trim(), code, StringComparison.OrdinalIgnoreCase);
}
