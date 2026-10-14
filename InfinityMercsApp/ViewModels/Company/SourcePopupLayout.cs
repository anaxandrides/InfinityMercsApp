using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Company;

/// <summary>
/// The two-choice source popup. Compact fills the screen and stacks the cards with smaller artwork so both
/// choices and the back button fit; wider screens centre a capped card with the choices side by side.
/// </summary>
public sealed record SourcePopupLayout(
    LayoutOptions ModalHorizontalOptions,
    LayoutOptions ModalVerticalOptions,
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
        AdaptiveLayoutMode.Compact => new(LayoutOptions.Fill, LayoutOptions.Fill, double.PositiveInfinity, "*", "Auto,Auto", 0, 16, new(1, 0), 96, 150),
        _ => new(LayoutOptions.Center, LayoutOptions.Center, mode == AdaptiveLayoutMode.Medium ? 700 : 820, "*,*", "*", 16, 0, new(0, 1), 140, 240)
    };
}
