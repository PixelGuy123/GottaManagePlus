using System.Collections;
using System.Collections.Generic;

namespace GottaManagePlus.Utils;

public static class CollectionUtils
{
    /// <summary>
    /// Whether the index is not outside the collection.
    /// </summary>
    /// <param name="list">The collection to be checked.</param>
    /// <param name="index">The index to be checked.</param>
    /// <returns><see langword="true"/> if the index is in bounds; otherwise, <see langword="false"/>.</returns>
    public static bool IsIndexInBounds<T>(this IReadOnlyCollection<T> list, int index) => index >= 0 && index < list.Count;
}