namespace InfinityMercsApp.ViewModels;

/// <summary>Which unit's details to show, and which of its configurations to list.</summary>
/// <param name="MercsOnly">Hide configurations that cost SWC, and the SWC label.</param>
/// <param name="LieutenantOnly">List only lieutenant configurations.</param>
public sealed record UnitDetailRequest(
    int FactionId,
    int UnitId,
    string UnitName,
    string? LogoSource = null,
    bool MercsOnly = false,
    bool LieutenantOnly = false);
