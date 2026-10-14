using InfinityMercsApp.Domain.Models.Army.SpecOps;
using InfinityMercsApp.Infrastructure.Models.Database.Army.SpecOps;

namespace InfinityMercsApp.Infrastructure.Data;

/// <summary>
/// The rows of the spec-ops link tables, and the mapping between them and <see cref="SpecopsChoiceLinks"/>.
/// </summary>
public sealed class SpecopsLinkRows
{
    public const string ExtrasKind = "extras";
    public const string SkillsKind = "skills";
    public const string EquipKind = "equip";
    public const string WeaponsKind = "weapons";

    public List<SpecopsLinkRow> Links { get; } = [];

    public List<SpecopsLinkExtraRow> LinkExtras { get; } = [];

    /// <summary>Adds the rows for the choice stored under <paramref name="ownerKey"/>.</summary>
    public void Add(int factionId, string ownerKey, SpecopsChoiceLinks links)
    {
        for (var position = 0; position < links.Extras.Count; position++)
        {
            Links.Add(new SpecopsLinkRow
            {
                FactionId = factionId,
                OwnerKey = ownerKey,
                LinkKind = ExtrasKind,
                Position = position,
                LinkedId = links.Extras[position]
            });
        }

        AddLinks(factionId, ownerKey, SkillsKind, links.Skills);
        AddLinks(factionId, ownerKey, EquipKind, links.Equip);
        AddLinks(factionId, ownerKey, WeaponsKind, links.Weapons);
    }

    /// <summary>Rebuilds the links of every choice the rows describe, keyed by owner key.</summary>
    public Dictionary<string, SpecopsChoiceLinks> Assemble()
    {
        var extrasByLink = LinkExtras
            .GroupBy(row => (row.OwnerKey, row.LinkKind, row.LinkPosition))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)group.OrderBy(row => row.Position).Select(row => row.ExtraId).ToList());

        var result = new Dictionary<string, SpecopsChoiceLinks>(StringComparer.Ordinal);
        foreach (var owner in Links.GroupBy(row => row.OwnerKey, StringComparer.Ordinal))
        {
            var byKind = owner.ToLookup(row => row.LinkKind, StringComparer.Ordinal);
            IReadOnlyList<SpecopsLink> LinksOf(string kind) => byKind[kind]
                .OrderBy(row => row.Position)
                .Select(row => new SpecopsLink(
                    row.LinkedId,
                    extrasByLink.TryGetValue((owner.Key, kind, row.Position), out var extras) ? extras : []))
                .ToList();

            result[owner.Key] = new SpecopsChoiceLinks(
                byKind[ExtrasKind].OrderBy(row => row.Position).Select(row => row.LinkedId).ToList(),
                LinksOf(SkillsKind),
                LinksOf(EquipKind),
                LinksOf(WeaponsKind));
        }

        return result;
    }

    private void AddLinks(int factionId, string ownerKey, string kind, IReadOnlyList<SpecopsLink> links)
    {
        for (var position = 0; position < links.Count; position++)
        {
            var link = links[position];
            Links.Add(new SpecopsLinkRow
            {
                FactionId = factionId,
                OwnerKey = ownerKey,
                LinkKind = kind,
                Position = position,
                LinkedId = link.Id
            });

            for (var extraPosition = 0; extraPosition < link.Extras.Count; extraPosition++)
            {
                LinkExtras.Add(new SpecopsLinkExtraRow
                {
                    FactionId = factionId,
                    OwnerKey = ownerKey,
                    LinkKind = kind,
                    LinkPosition = position,
                    Position = extraPosition,
                    ExtraId = link.Extras[extraPosition]
                });
            }
        }
    }
}
