using InfinityMercsApp.Domain.Company;

namespace InfinityMercsApp.Presentation.Units;

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
    /// The icons in column order: the order type (irregular over regular), lieutenants, peripheral, impetuous,
    /// tactical awareness, cube, cube 2.0 and hackable, at most <see cref="MaxShown"/>.
    /// </summary>
    public IReadOnlyList<HeaderIcon> Shown
    {
        get
        {
            var icons = new List<HeaderIcon>();
            if (Irregular)
            {
                icons.Add(Icon("irregular.svg", "Irregular: gives an Irregular order, for itself only"));
            }
            else if (Regular)
            {
                icons.Add(Icon("regular.svg", "Regular: adds a Regular order to the order pool"));
            }

            icons.AddRange(Enumerable.Repeat(Icon("lieutenant.svg", "Lieutenant: gives a Lieutenant order"), Math.Max(0, Lieutenants)));
            AddIf(icons, Peripheral, "peripheral.svg", "Peripheral");
            AddIf(icons, Impetuous, "impetuous.svg", "Impetuous");
            AddIf(icons, TacticalAwareness, "tactical.svg", "Tactical Awareness: gives an extra Regular order for itself");
            AddIf(icons, Cube, "cube.svg", "Cube");
            AddIf(icons, Cube2, "cube2.svg", "Cube 2.0");
            AddIf(icons, Hackable, "hackable.svg", "Hackable: can be targeted by hacking programs");
            return icons.Take(MaxShown).ToList();
        }
    }

    /// <summary>The icon files in column order.</summary>
    public IReadOnlyList<string> Sources => Shown.Select(icon => icon.Source).ToList();

    /// <summary>What each icon means, one per line in column order, for the column's tooltip.</summary>
    public string Meanings => string.Join(Environment.NewLine, Shown.Select(icon => icon.Meaning));

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

    private static HeaderIcon Icon(string file, string meaning) => new(Folder + file, meaning);

    private static void AddIf(List<HeaderIcon> icons, bool show, string file, string meaning)
    {
        if (show)
        {
            icons.Add(Icon(file, meaning));
        }
    }
}
