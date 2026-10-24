using System.Globalization;
using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Converters;

/// <summary>Turns a layout's <see cref="Inset"/> into a Thickness.</summary>
public sealed class InsetConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Inset inset ? new Thickness(inset.Left, inset.Top, inset.Right, inset.Bottom) : Thickness.Zero;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
