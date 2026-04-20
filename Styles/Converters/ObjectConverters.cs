using Avalonia.Data.Converters;

namespace GottaManagePlus.Styles.Converters;

public static class ObjectConverters
{
    public static readonly FuncValueConverter<object?, double> ObjectNotNullToOpacityIndicator =
        new(obj => obj != null ? 1d : 0.5d);
}