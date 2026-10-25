using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Presentation.Company;

/// <summary>
/// The two-choice source popup. Compact fills the screen and stacks the cards with smaller artwork so both
/// choices and the back button fit; wider screens centre a capped card with the choices side by side.
/// </summary>
public sealed record SourcePopupLayout(
    Alignment ModalHorizontalOptions,
    Alignment ModalVerticalOptions,
    double ModalMaximumWidth,
    string CardsColumns,
    string CardsRows,
    double CardsColumnSpacing,
    double CardsRowSpacing,
    GridCell SecondCardCell,
    double IconHeight,
    double CardMinimumHeight)
{
    public static SourcePopupLayout For(AdaptiveLayoutMode mode) => mode switch
    {
        AdaptiveLayoutMode.Compact => new(Alignment.Fill, Alignment.Fill, double.PositiveInfinity, "*", "Auto,Auto", 0, 16, new(1, 0), 96, 150),
        _ => new(Alignment.Center, Alignment.Center, mode == AdaptiveLayoutMode.Medium ? 700 : 820, "*,*", "*", 16, 0, new(0, 1), 140, 240)
    };
}
