using System.Text;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

public readonly record struct UnitOrderTraits(bool HasRegular, bool HasIrregular, bool HasImpetuous, bool HasTacticalAwareness);

public readonly record struct UnitTechTraits(bool HasCube, bool HasCube2, bool HasHackable);

/// <summary>Order types as the API writes them on an option's orders.</summary>
public static class OrderTypes
{
    public const string Regular = "REGULAR";
    public const string Irregular = "IRREGULAR";
    public const string Impetuous = "IMPETUOUS";

    /// <summary>A tactical order: the trooper has Tactical Awareness.</summary>
    public const string Tactical = "TACTICAL";

    public const string Lieutenant = "LIEUTENANT";
}

/// <summary>
/// Unit-wide badges shown next to a unit: its order types and its Cube/Hackable traits.
/// </summary>
public static class UnitTraits
{
    /// <summary>
    /// Regular/Irregular/Impetuous when any option gives that order; Tactical Awareness only when every
    /// option gives a TACTICAL order.
    /// </summary>
    public static UnitOrderTraits ReadOrderTraits(UnitProfileGroups groups)
    {
        var hasRegular = false;
        var hasIrregular = false;
        var hasImpetuous = false;
        var optionsSeen = 0;
        var tacticalOptions = 0;

        foreach (var (_, option) in groups.AllOptions())
        {
            optionsSeen++;
            var optionTraits = ReadOrderTraits(option);
            hasRegular |= optionTraits.HasRegular;
            hasIrregular |= optionTraits.HasIrregular;
            hasImpetuous |= optionTraits.HasImpetuous;
            if (optionTraits.HasTacticalAwareness)
            {
                tacticalOptions++;
            }
        }

        return new UnitOrderTraits(hasRegular, hasIrregular, hasImpetuous, optionsSeen > 0 && tacticalOptions == optionsSeen);
    }

    /// <summary>The order types one option gives (a TACTICAL order means Tactical Awareness).</summary>
    public static UnitOrderTraits ReadOrderTraits(ProfileOption option)
    {
        bool Gives(string orderType) =>
            option.Orders.Any(order => string.Equals(order.Type, orderType, StringComparison.OrdinalIgnoreCase));

        return new UnitOrderTraits(
            Gives(OrderTypes.Regular),
            Gives(OrderTypes.Irregular),
            Gives(OrderTypes.Impetuous),
            Gives(OrderTypes.Tactical));
    }

    /// <summary>
    /// Cube / Cube 2.0 / Hackable from one option's equipment, skill and characteristic names. Unlike the
    /// unit-wide badge, any hacking device also makes the option Hackable.
    /// </summary>
    public static UnitTechTraits ReadTechTraits(IEnumerable<string> optionNames)
    {
        var traits = new TechTraitAccumulator(hackingDevicesAreHackable: true);
        foreach (var name in optionNames)
        {
            traits.AddName(name);
        }

        return new UnitTechTraits(traits.HasCube, traits.HasCube2, traits.HasHackable);
    }

    /// <summary>
    /// Cube / Cube 2.0 / Hackable from the equipment, skill and characteristic names of every profile and
    /// option (without following includes).
    /// </summary>
    public static UnitTechTraits ReadTechTraits(
        UnitProfileGroups groups,
        IReadOnlyDictionary<int, string> equipNames,
        IReadOnlyDictionary<int, string> skillNames,
        IReadOnlyDictionary<int, string> charNames)
    {
        var traits = new TechTraitAccumulator(hackingDevicesAreHackable: false);
        foreach (var group in groups.Groups)
        {
            foreach (var profile in group.Profiles)
            {
                traits.Add(profile.Equip, equipNames);
                traits.Add(profile.Skills, skillNames);
                traits.Add(profile.Chars, charNames);
            }

            foreach (var option in group.Options)
            {
                traits.Add(option.Equip, equipNames);
                traits.Add(option.Skills, skillNames);
                traits.Add(option.Chars, charNames);
            }
        }

        return new UnitTechTraits(traits.HasCube, traits.HasCube2, traits.HasHackable);
    }

    private sealed class TechTraitAccumulator(bool hackingDevicesAreHackable)
    {
        public bool HasCube { get; private set; }

        public bool HasCube2 { get; private set; }

        public bool HasHackable { get; private set; }

        public void Add(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup)
        {
            foreach (var entry in entries)
            {
                if (entry.Id is int id && lookup.TryGetValue(id, out var name))
                {
                    AddName(name);
                }
            }
        }

        public void AddName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var normalized = NormalizeTokenText(name);
            if (Regex.IsMatch(normalized, @"\bhackable\b", RegexOptions.IgnoreCase) ||
                (hackingDevicesAreHackable && IsHackingDevice(normalized)))
            {
                HasHackable = true;
            }

            if (Regex.IsMatch(normalized, @"\b(no[\s-]*cube|without[\s-]*cube|cube[\s-]*none)\b", RegexOptions.IgnoreCase))
            {
                return;
            }

            if (Regex.IsMatch(normalized, @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase))
            {
                HasCube2 = true;
                return;
            }

            if (Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase))
            {
                HasCube = true;
            }
        }
    }

    private static bool IsHackingDevice(string normalizedName) =>
        Regex.IsMatch(normalizedName, @"\bhacking\s*device\b", RegexOptions.IgnoreCase) ||
        Regex.IsMatch(normalizedName, @"\bhacking\s*device\s*plus\b|\bhd\s*\+\b", RegexOptions.IgnoreCase);

    private static string NormalizeTokenText(string value)
    {
        var lowered = value.ToLowerInvariant();
        var sb = new StringBuilder(lowered.Length);
        foreach (var c in lowered)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '.' ? c : ' ');
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }
}
