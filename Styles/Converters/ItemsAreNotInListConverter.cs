using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;

namespace GottaManagePlus.Styles.Converters;

public class ItemsAreNotInListConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // Expects parameter to exist and be a list.
        if (values.FirstOrDefault() is not IList list) return AvaloniaProperty.UnsetValue;

        // Check if every value is contained in list.
        return !values.Skip(1).All(list.Contains);
    }
}