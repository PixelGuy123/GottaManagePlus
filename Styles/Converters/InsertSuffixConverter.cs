using System.Globalization;
using Avalonia.Data.Converters;

namespace GottaManagePlus.Styles.Converters;

public class InsertSuffixConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        $"{value} {parameter}";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}