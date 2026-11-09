using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Proofreader.Core.Models;

namespace Proofreader.App.Converters;

public class ErrorTypeToColorConverter : IValueConverter
{
    // Синглтон для использования в XAML
    public static readonly ErrorTypeToColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ErrorType errorType)
        {
            return errorType switch
            {
                ErrorType.Spelling => Colors.Red,
                ErrorType.Punctuation => Colors.Orange,
                ErrorType.Grammar => Colors.Gold,
                ErrorType.Style => Colors.DodgerBlue,
                _ => Colors.Red
            };
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}