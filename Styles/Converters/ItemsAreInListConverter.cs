using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using Serilog;

namespace GottaManagePlus.Styles.Converters;

public class ItemsAreInListConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        foreach (var value in values)
            Log.Logger.Information("Got as value: {0}", value);
        // Expects parameter to exist and be a list.
        if (values.FirstOrDefault() is not IList list) return AvaloniaProperty.UnsetValue;

        // Check if every value is contained in list.
        return values.Skip(1).All(list.Contains);
    }
}