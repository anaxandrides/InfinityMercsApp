namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>A list shown in two columns, filled left, right, left...; an empty list shows "(none)" on the left.</summary>
public sealed record ProfileColumns(IReadOnlyList<ProfileLine> Left, IReadOnlyList<ProfileLine> Right)
{
    public static ProfileColumns Empty { get; } = From([]);

    public static ProfileColumns From(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return new([ProfileLine.None], []);
        }

        var items = lines.Select(line => new ProfileLine(line)).ToList();
        return new(
            items.Where((_, index) => index % 2 == 0).ToList(),
            items.Where((_, index) => index % 2 == 1).ToList());
    }
}
