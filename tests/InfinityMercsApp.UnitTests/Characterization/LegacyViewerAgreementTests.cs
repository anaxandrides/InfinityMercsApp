using System.Text.Json;
using InfinityMercsApp.UnitTests.Legacy;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// ViewerViewModel carries its own private copy of the option rules. These tests record where that copy
/// agrees with CompanyProfileOptionService / CompanyUnitFilterService on every real unit, which is what
/// allows both to be replaced by one shared implementation.
/// </summary>
public sealed class LegacyViewerAgreementTests
{
    public static TheoryData<string> CorpusSources => new() { "army-101.json", "infinitymercs.db3" };

    [Theory]
    [MemberData(nameof(CorpusSources))]
    public void OptionRules_EveryRealOption_ViewerAndServiceAgree(string source)
    {
        var mismatches = new List<string>();
        var units = ReferenceCorpus.All.Where(x => x.Source == source).ToList();
        Assert.NotEmpty(units);

        foreach (var unit in units)
        {
            var skills = CorpusLookups.For(unit.ArmyFiltersJson).Skills;
            using var doc = JsonDocument.Parse(unit.ProfileGroupsJson);
            var root = doc.RootElement;
            foreach (var group in root.EnumerateArray())
            {
                Check(mismatches, unit, "controller",
                    LegacyCompanyProfileOptionService.IsControllerGroup(root, group),
                    LegacyViewerOptionRules.IsControllerGroup(root, group));

                foreach (var option in group.GetProperty("options").EnumerateArray())
                {
                    Check(mismatches, unit, "swc",
                        LegacyCompanyProfileOptionService.ReadOptionSwc(option),
                        LegacyViewerOptionRules.ReadOptionSwc(option));
                    Check(mismatches, unit, "cost",
                        LegacyCompanyProfileOptionService.ReadOptionCost(option),
                        LegacyViewerOptionRules.ReadOptionCost(option));
                    Check(mismatches, unit, "adjusted-cost",
                        LegacyCompanyProfileOptionService.ReadAdjustedOptionCost(root, group, option),
                        LegacyViewerOptionRules.ReadAdjustedOptionCost(root, group, option));
                    Check(mismatches, unit, "parsed-cost",
                        LegacyCompanyUnitFilterService.ParseCostValue(LegacyCompanyProfileOptionService.ReadAdjustedOptionCost(root, group, option)),
                        LegacyViewerOptionRules.ParseCostValue(LegacyViewerOptionRules.ReadAdjustedOptionCost(root, group, option)));
                    Check(mismatches, unit, "lieutenant",
                        LegacySharedUtilities.IsLieutenantOption(option, skills),
                        LegacyViewerOptionRules.IsLieutenantOption(option, skills));
                }
            }

            Check(mismatches, unit, "visible-zero-swc",
                LegacyCompanyUnitFilterService.UnitHasVisibleOption(unit.ProfileGroupsJson, skills, false, true),
                LegacyViewerOptionRules.UnitHasVisibleOptionWithFilter(
                    unit.ProfileGroupsJson,
                    skills,
                    new Dictionary<int, string>(),
                    new Dictionary<int, string>(),
                    new Dictionary<int, string>(),
                    new Dictionary<int, string>(),
                    UnitFilterCriteria.None,
                    requireLieutenant: false,
                    requireZeroSwc: true));

            if (mismatches.Count > 20)
            {
                break;
            }
        }

        Assert.True(mismatches.Count == 0, string.Join(Environment.NewLine, mismatches));
    }

    private static void Check<T>(List<string> mismatches, CorpusUnit unit, string what, T service, T viewer)
    {
        if (!EqualityComparer<T>.Default.Equals(service, viewer))
        {
            mismatches.Add($"{unit.Key} {what}: service='{service}' viewer='{viewer}'");
        }
    }
}
