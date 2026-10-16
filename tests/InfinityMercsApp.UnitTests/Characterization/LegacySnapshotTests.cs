using System.Text;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Records everything the legacy rules derive from the PanOceania payload into an approved snapshot
/// (<c>tests/InfinityMercsApp.UnitTests/Snapshots/army-101.legacy.approved.txt</c>).
/// <para>
/// First run on a machine without the approved file: the test writes it and fails once, asking you to
/// review and commit it. Afterwards any change in output writes a <c>.received.txt</c> next to it and fails.
/// To deliberately accept new output, replace the approved file with the received one.
/// </para>
/// </summary>
public sealed class LegacySnapshotTests
{
    private const string ApprovedRelativePath = "tests/InfinityMercsApp.UnitTests/Snapshots/army-101.legacy.approved.txt";

    [Fact]
    public void Army101_LegacyRuleOutput_MatchesApprovedSnapshot()
    {
        var actual = BuildArmy101Snapshot();
        var approvedPath = Path.Combine(RepoFiles.RepoRoot(), ApprovedRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var receivedPath = Path.ChangeExtension(approvedPath, null).Replace(".approved", ".received") + ".txt";

        if (!File.Exists(approvedPath))
        {
            if (Path.GetDirectoryName(approvedPath) is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(approvedPath, actual, new UTF8Encoding(false));
            Assert.Fail($"No approved snapshot existed, so one was written to '{approvedPath}'. Review it, commit it, and re-run.");
        }

        var approved = NormalizeNewlines(File.ReadAllText(approvedPath));
        if (approved == actual)
        {
            if (File.Exists(receivedPath))
            {
                File.Delete(receivedPath);
            }

            return;
        }

        File.WriteAllText(receivedPath, actual, new UTF8Encoding(false));
        Assert.Fail(
            $"Legacy JSON rule output differs from the approved snapshot. First difference: {FirstDifference(approved, actual)}. " +
            $"Compare '{receivedPath}' with '{approvedPath}'.");
    }

    internal static string BuildArmy101Snapshot()
    {
        var sb = new StringBuilder();
        foreach (var unit in ReferenceCorpus.Army101)
        {
            sb.Append(LegacyProfileSnapshot.Build(unit));
        }

        return NormalizeNewlines(sb.ToString());
    }

    private static string NormalizeNewlines(string text) => text.Replace("\r\n", "\n");

    private static string FirstDifference(string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        for (var i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            var e = i < expectedLines.Length ? expectedLines[i] : "<end>";
            var a = i < actualLines.Length ? actualLines[i] : "<end>";
            if (e != a)
            {
                return $"line {i + 1}: expected '{e}' but was '{a}'";
            }
        }

        return "none";
    }
}
