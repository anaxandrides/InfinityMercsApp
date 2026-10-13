using System.Globalization;

namespace InfinityMercsApp.Converters;

/// <summary>Binds to the opposite of a bool, e.g. to show one view while another is hidden.</summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}
