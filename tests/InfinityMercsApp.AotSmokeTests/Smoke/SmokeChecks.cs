namespace InfinityMercsApp.AotSmokeTests.Smoke;

/// <summary>What the smoke test found: each value noted for the report, and each check that failed.</summary>
public sealed class SmokeChecks
{
    private readonly List<string> _lines = [];
    private readonly List<string> _failures = [];

    /// <summary>A value the report shows, so a run compiled ahead of time can be compared with a normal one.</summary>
    public void Note(string what, object? value) => _lines.Add($"{what}: {value}");

    public void Equal<T>(string what, T expected, T actual)
    {
        Note(what, actual);
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            _failures.Add($"{what}: expected {expected}, got {actual}");
        }
    }

    public void True(string what, bool condition) => Equal(what, true, condition);

    /// <summary>Writes the report; 0 when every check passed, else 1.</summary>
    public int Report(TextWriter output)
    {
        foreach (var line in _lines)
        {
            output.WriteLine(line);
        }

        foreach (var failure in _failures)
        {
            output.WriteLine($"FAILED {failure}");
        }

        output.WriteLine(_failures.Count == 0 ? "All smoke checks passed." : $"{_failures.Count} smoke checks failed.");
        return _failures.Count == 0 ? 0 : 1;
    }
}
