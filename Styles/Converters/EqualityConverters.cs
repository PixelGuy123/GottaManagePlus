using System.Collections;
using System.Linq;
using Avalonia.Data.Converters;

// ReSharper disable PossibleMultipleEnumeration

namespace GottaManagePlus.Styles.Converters;

public static class EqualityConverters
{
    /// <summary>
    /// Checks whether all arguments inside the array are equal to each other.
    /// </summary>
    public static readonly FuncMultiValueConverter<object?, bool> AllEqual =
        new(values =>
        {
            var first = values.FirstOrDefault();
            return values.Skip(1).All(v => first?.Equals(v) == true);
        });
    
    /// <summary>
    /// Checks whether at least one of the arguments inside the array are not equal to each other.
    /// </summary>
    public static readonly FuncMultiValueConverter<object?, bool> AtLeastOneNotEqual =
        new(values =>
        {
            var first = values.FirstOrDefault();
            return values.Skip(1).Any(v => first?.Equals(v) != true);
        });
    
    /// <summary>
    /// Checks whether at least one of the arguments inside the array are not equal to each other. Returns a <see langword="double"/>.
    /// </summary>
    public static readonly FuncMultiValueConverter<object?, double> AtLeastOneNotEqualDouble =
        new(values =>
        {
            var first = values.FirstOrDefault();
            return values.Skip(1).Any(v => first?.Equals(v) != true) ? 1.0 : 0.0;
        });
}