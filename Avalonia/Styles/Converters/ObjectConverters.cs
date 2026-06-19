using Avalonia.Data.Converters;

namespace GottaManagePlus.Styles.Converters;

public static class ObjectConverters
{
    public static readonly FuncValueConverter<object?, double> ObjectNotNullToOpacityIndicator =
        new(obj =>
        {
            if (obj is string str)
                return !string.IsNullOrEmpty(str) ? 1d : 0.5d;
            return obj != null ? 1d : 0.5d;
        });
}