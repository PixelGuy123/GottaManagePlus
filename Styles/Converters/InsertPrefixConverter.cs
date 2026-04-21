using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace GottaManagePlus.Styles.Converters;

public class InsertPrefixConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)=>
        $"{parameter} {value}";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}