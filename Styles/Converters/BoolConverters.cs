using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GottaManagePlus.Styles.Converters;

public static class BoolConverters
{
    public static readonly FuncValueConverter<bool, string> BoolToCheckmarkIconPath =
        new(flag => flag ? "/Assets/UI/checkmark.svg" : "/Assets/UI/cancel.svg");
    
    public static readonly FuncValueConverter<bool, IBrush?> BoolToYCTPError =
        new(flag => flag ? 
            Application.Current!.Resources["YctpBg"] as IBrush : // Green Success
    Application.Current!.Resources["YctpBg-Error"] as IBrush); // Red Error/Disable
    
    public static readonly FuncValueConverter<bool, IBrush?> BoolToYCTPWarning =
        new(flag => flag ? 
            Application.Current!.Resources["YctpBg"] as IBrush : // Green Success
            Application.Current!.Resources["YctpBg-Warning"] as IBrush); // Warning
}