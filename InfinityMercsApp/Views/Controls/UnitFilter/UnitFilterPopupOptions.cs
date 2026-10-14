namespace InfinityMercsApp.Views.Controls.UnitFilter;

public sealed class UnitFilterPopupOptions
{
    public List<string> Classification { get; init; } = [];
    public List<string> Characteristics { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<string> Equipment { get; init; } = [];
    public List<string> Weapons { get; init; } = [];
    public List<string> Ammo { get; init; } = [];
    public int MinPoints { get; init; }
    public int MaxPoints { get; init; } = 200;
}
