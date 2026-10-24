using System.Globalization;
using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Converters;

/// <summary>Turns a layout's <see cref="Alignment"/> into the LayoutOptions or TextAlignment the bound property takes.</summary>
public sealed class AlignmentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var alignment = value as Alignment? ?? Alignment.Fill;
        if (targetType == typeof(TextAlignment))
        {
            return alignment switch
            {
                Alignment.Center => TextAlignment.Center,
                Alignment.End => TextAlignment.End,
                _ => TextAlignment.Start
            };
        }

        return alignment switch
        {
            Alignment.Start => LayoutOptions.Start,
            Alignment.Center => LayoutOptions.Center,
            Alignment.End => LayoutOptions.End,
            _ => LayoutOptions.Fill
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
