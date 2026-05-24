using Avalonia.Data.Converters;

namespace GottaManagePlus.Styles.Converters;

public static class DateConverters
{
    public static readonly FuncValueConverter<DateOnly, string> ToShortDate =
        new(only => only.ToShortDateString());

    public static readonly FuncValueConverter<DateTime, string> ToDateOnly =
        new(date => date.ToShortDateString());
}