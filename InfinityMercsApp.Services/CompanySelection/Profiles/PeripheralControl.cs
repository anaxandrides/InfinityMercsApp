namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>A peripheral its controller commands (synchronized, control or cyberplug): it is Irregular and gives FT Master.</summary>
public static class PeripheralControl
{
    /// <summary>Whether <paramref name="text"/> names a controlled peripheral.</summary>
    public static bool Mentions(string? text) =>
        !string.IsNullOrWhiteSpace(text) &&
        (text.Contains("synchronized", StringComparison.OrdinalIgnoreCase) ||
         text.Contains("control", StringComparison.OrdinalIgnoreCase) ||
         text.Contains("cyberplug", StringComparison.OrdinalIgnoreCase));

    /// <summary>The skill a controller of <paramref name="peripheralName"/> gets.</summary>
    public static string FtMaster(string peripheralName) => $"FT Master ({peripheralName})";
}
