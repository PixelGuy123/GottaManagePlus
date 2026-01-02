using System;
using Avalonia.Data.Converters;

namespace GottaManagePlus.Styles.Converters;

public static class FontConverters
{
    /// <summary>
    /// Gets a Converter that takes a string as input and converts it into a size estimation.
    /// </summary>
    public static FuncValueConverter<string, double> Max20TextToSizeConverter { get; } = 
        new(str => 
            string.IsNullOrEmpty(str) ? 20d : 
            Math.Max(10d, 20d - (str.Length - 1) * 0.35d));
}