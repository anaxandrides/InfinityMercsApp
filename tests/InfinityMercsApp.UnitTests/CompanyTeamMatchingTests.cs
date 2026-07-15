using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.UnitTests;

/// <summary>
/// Characterization tests for <see cref="CompanyTeamMatchingWorkflow.ResolveUnitForTeamEntry{TUnit}"/>
/// and its O(1) counterpart, <see cref="TeamUnitResolver{TUnit}"/>. Every case asserts an expected
/// unit id derived by hand-tracing the tiered matcher, then checks both implementations agree with it,
/// so the two can never silently diverge.
/// </summary>
public sealed class CompanyTeamMatchingTests
{
    private sealed record TestUnit(int Id, string? Name, string? Slug);

    private static readonly TestUnit[] Units =
    [
        new(1, "Van Zant", "van-zant"),
        new(2, "Order Sergeant", "order-sergeant"),
        new(3, "Order Sergeant REINF", "order-sergeant-reinf"),
        new(4, "Order Sergeant", "order-sergeant-duplicate"),
        new(5, "Ärchäic Trooper", null),
        new(6, "---", "dash-only-slug-6"),
        new(7, "Support Trooper", "support-trooper"),
        new(8, "Naga Squad", "naga_squad"),
        new(9, "Wulver", "wulver-alpha"),
        new(10, "Dup A", "dup-slug"),
        new(11, "Dup B", "dup-slug"),
        new(12, "Dup IC First", "Dup-Slug-IC"),
        new(13, "Dup IC Second", "dup-slug-ic"),
    ];

    private static void AssertBothResolveTo(string? allowedProfileName, string? allowedProfileSlug, int? expectedId)
    {
        var oldResult = CompanyTeamMatchingWorkflow.ResolveUnitForTeamEntry(
            allowedProfileName,
            allowedProfileSlug,
            Units,
            x => x.Name,
            x => x.Slug);
        Assert.Equal(expectedId, oldResult?.Id);

        var resolver = TeamUnitResolver<TestUnit>.Build(Units, x => x.Name, x => x.Slug);
        var newResult = resolver.Resolve(allowedProfileName, allowedProfileSlug);
        Assert.Equal(expectedId, newResult?.Id);
    }

    [Fact]
    public void SlugExactOrdinalMatch_ReturnsUnit()
    {
        AssertBothResolveTo("ignored name", "van-zant", 1);
    }

    [Fact]
    public void SlugCaseInsensitiveMatch_ReturnsUnit()
    {
        AssertBothResolveTo("ignored name", "VAN-ZANT", 1);
    }

    [Fact]
    public void SlugNormalizedMatch_ReturnsUnit()
    {
        // "NAGA SQUAD" (space) shares no raw-string equality with "naga_squad" (underscore) under
        // Ordinal or OrdinalIgnoreCase, but both normalize to "naga-squad".
        AssertBothResolveTo("Naga Squad", "NAGA SQUAD", 8);
    }

    [Fact]
    public void SlugFailsButNameMatchesExactly_ReturnsUnit()
    {
        AssertBothResolveTo("Wulver", "totally-unrelated-slug", 9);
    }

    [Fact]
    public void SlugMatchesOnlyInReinforcementFallbackPartition_ReturnsUnit()
    {
        // Query name contains REINF (reinforcementOnly=true) but the slug belongs to a
        // non-reinforcement unit, reachable only through the fallback tier.
        AssertBothResolveTo("Some REINF Team Entry", "SUPPORT-TROOPER", 7);
    }

    [Fact]
    public void SlugNoMatchAnywhere_ReturnsNull()
    {
        AssertBothResolveTo("Nonexistent Name Xyz", "nonexistent-xyz", null);
    }

    [Fact]
    public void DuplicateExactSlug_FirstOccurrenceWins()
    {
        AssertBothResolveTo(null, "dup-slug", 10);
    }

    [Fact]
    public void DuplicateSlugDifferingOnlyByCase_FirstOccurrenceWins()
    {
        AssertBothResolveTo(null, "DUP-SLUG-IC", 12);
    }

    [Fact]
    public void WhitespaceOnlySlug_FallsBackToNamePath()
    {
        AssertBothResolveTo("Van Zant", "   ", 1);
    }

    [Fact]
    public void NameExactCaseInsensitiveMatch_ReturnsUnit()
    {
        AssertBothResolveTo("order sergeant", null, 2);
    }

    [Fact]
    public void DuplicateExactName_FirstOccurrenceWins()
    {
        // Units 2 and 4 share the name "Order Sergeant"; unit 2 was enumerated first.
        AssertBothResolveTo("ORDER SERGEANT", null, 2);
    }

    [Fact]
    public void NameNormalizedMatch_StripsDiacriticsAndCase()
    {
        AssertBothResolveTo("ARCHAIC TROOPER", null, 5);
    }

    [Fact]
    public void NameNormalizesToEmpty_ReturnsNullEvenWithoutExactMatch()
    {
        // "###" has no letters/digits, so NormalizeTeamUnitName yields "" and the method
        // returns null immediately rather than looping the normalized-name tier.
        AssertBothResolveTo("###", null, null);
    }

    [Fact]
    public void NameOnlyReinforcementQueryWithNoMatch_ReturnsNull()
    {
        AssertBothResolveTo("Nonexistent REINF Unit", null, null);
    }

    [Fact]
    public void BothNameAndSlugMissing_ReturnsNull()
    {
        AssertBothResolveTo(null, null, null);
    }

    [Fact]
    public void NullSlugUnit_NeverMatchedBySlugTiers()
    {
        // Unit 5 has a null slug; querying by a slug that happens to equal its name must not match.
        AssertBothResolveTo(null, "Ärchäic Trooper", null);
    }
}
