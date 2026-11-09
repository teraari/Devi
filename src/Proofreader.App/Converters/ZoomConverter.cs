using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Proofreader.App.Converters;

public class ZoomConverter : IValueConverter
{
    public static readonly ZoomConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double zoom)
        {
            // Базовый шрифт 14 умножаем на коэффициент зума
            return 14.0 * zoom;
        }
        return 14.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}