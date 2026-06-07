using Avalonia.Data.Converters;

namespace GottaManagePlus.Styles.Converters;

public static class BoolConverters
{
    public static readonly FuncValueConverter<bool, string> BoolToCheckmarkIconPath =
        new(flag => flag ? "/Assets/UI/checkmark.svg" : "/Assets/UI/cancel.svg");
    
    public static readonly FuncValueConverter<bool, double> BoolToOpacityIndicator =
        new(flag => flag ? 1d : 0.5d);
    
    // TODO: Add localization here
    public static readonly FuncValueConverter<bool, string?> BoolToCheckBoxFileMissingReason =
        new(flag => flag ? null : "This mod has no file available for the current version of the game.");
}