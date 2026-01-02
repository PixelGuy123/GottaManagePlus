using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GottaManagePlus.Styles.Converters;

public static class ForegroundConverters
{
    /// <summary>
    /// Gets a Converter that takes a string as input and converts it into a size estimation.
    /// </summary>
    public static FuncValueConverter<bool, IBrush> BoolToColor { get; } = 
        new(boolean => boolean ? Brushes.LimeGreen : Brushes.Red);
}