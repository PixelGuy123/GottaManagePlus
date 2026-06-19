using Avalonia.Data.Converters;

namespace GottaManagePlus.Styles.Converters;

public static class ListStringConverters
{
    public static readonly FuncValueConverter<IList<string?>, string> ConvertToString =
        new(list => string.Join('|', list ?? []));
}