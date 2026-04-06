using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GottaManagePlus.Styles.Converters;

public static class ForegroundConverters
{
    /// <summary>
    /// Gets a Converter that translates <see langword="true"/> to <c>LimeGreen</c>
    /// and <see langword="false"/> to <c>Red</c>  
    /// </summary>
    public static FuncValueConverter<bool, IBrush> BoolToColor { get; } = 
        new(boolean => boolean ? Brushes.LimeGreen : Brushes.Red);
}