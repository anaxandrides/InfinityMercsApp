using System.Globalization;

namespace InfinityMercsApp.Converters;

/// <summary>Turns an HTML page into what a WebView shows.</summary>
public sealed class HtmlSourceConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new HtmlWebViewSource { Html = value as string ?? string.Empty };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
