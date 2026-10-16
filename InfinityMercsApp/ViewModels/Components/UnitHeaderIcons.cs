using InfinityMercsApp.Domain.Company;

namespace InfinityMercsApp.ViewModels.Components;

/// <summary>The trait icons in the unit header's icon column.</summary>
public sealed record UnitHeaderIcons
{
    /// <summary>The column has room for six icons; any more are left off.</summary>
    public const int MaxShown = 6;

    private const string Folder = "SVGCache/CBIcons/";

    public bool Regular { get; init; }
    public bool Irregular { get; init; }

    /// <summary>One lieutenant icon per lieutenant order.</summary>
    public int Lieutenants { get; init; }

    public bool Peripheral { get; init; }
    public bool Impetuous { get; init; }
    public bool TacticalAwareness { get; init; }
    public bool Cube { get; init; }
    public bool Cube2 { get; init; }
    public bool Hackable { get; init; }

    public static UnitHeaderIcons None { get; } = new();

    /// <summary>
    /// The icon files in column order: the order type (irregular over regular), lieutenants, peripheral,
    /// impetuous, tactical awareness, cube, cube 2.0 and hackable, at most <see cref="MaxShown"/>.
    /// </summary>
    public IReadOnlyList<string> Sources
    {
        get
        {
            var sources = new List<string>();
            if (Irregular)
            {
                sources.Add(Folder + "irregular.svg");
            }
            else if (Regular)
            {
                sources.Add(Folder + "regular.svg");
            }

            sources.AddRange(Enumerable.Repeat(Folder + "lieutenant.svg", Math.Max(0, Lieutenants)));
            AddIf(sources, Peripheral, "peripheral.svg");
            AddIf(sources, Impetuous, "impetuous.svg");
            AddIf(sources, TacticalAwareness, "tactical.svg");
            AddIf(sources, Cube, "cube.svg");
            AddIf(sources, Cube2, "cube2.svg");
            AddIf(sources, Hackable, "hackable.svg");
            return sources.Take(MaxShown).ToList();
        }
    }

    public bool HasAny => Sources.Count > 0;

    /// <summary>A saved trooper's icons: its traits plus <paramref name="lieutenants"/> lieutenant icons.</summary>
    public static UnitHeaderIcons From(UnitTraitIcons traits, int lieutenants) => new()
    {
        Regular = traits.Regular,
        Irregular = traits.Irregular,
        Lieutenants = lieutenants,
        Peripheral = traits.Peripheral,
        TacticalAwareness = traits.TacticalAwareness,
        Cube = traits.Cube,
        Cube2 = traits.Cube2,
        Hackable = traits.Hackable
    };

    private static void AddIf(List<string> sources, bool show, string file)
    {
        if (show)
        {
            sources.Add(Folder + file);
        }
    }
}
