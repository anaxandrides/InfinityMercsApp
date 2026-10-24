namespace InfinityMercsApp.Presentation.Themes;

/// <summary>The accent colours of the equipment and skills summaries.</summary>
internal sealed record SummaryAccents(Color Equipment, Color Skills)
{
    public static SummaryAccents OnLightBackground { get; } = new(Color.FromArgb("#0B5563"), Color.FromArgb("#7C2D12"));

    public static SummaryAccents OnDarkBackground { get; } = new(Color.FromArgb("#67E8F9"), Color.FromArgb("#FDE68A"));
}
