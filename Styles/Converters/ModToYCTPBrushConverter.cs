using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GottaManagePlus.Styles.Converters;

public class ModToYCTPBrushConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2)
            return new BindingNotification(new ArgumentException("Values given is below or above 2."), 
                BindingErrorType.DataValidationError);

        if (values[0] == null || values[1] == null)
            return Application.Current!.Resources["YctpBg-Warning"] as IBrush;

        return values[0]?.Equals(values[1]) == true
            ? Application.Current!.Resources["YctpBg"] as IBrush
            : Application.Current!.Resources["YctpBg-Warning"] as IBrush;
    }
}