using System;
using System.Collections;
using System.Collections.Generic;

namespace GottaManagePlus.Utils.Collections;

/// <summary>
/// A generic list that automatically maintains sorted order after each modification.
/// </summary>
public class AutoSortedList<T> : IList<T>
{
    // --- Private Fields ---
    private readonly List<T> _internalList;

    // --- Constructors ---
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoSortedList{T}"/> class that is empty.
    /// </summary>
    public AutoSortedList() => _internalList = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoSortedList{T}"/> class that contains elements copied from the specified collection and sorts them.
    /// </summary>
    /// <param name="enumerable">The collection whose elements are copied to the new list.</param>
    public AutoSortedList(IEnumerable<T> enumerable) : this()
    {
        _internalList = [.. enumerable];
        _internalList.Sort();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoSortedList{T}"/> class that is empty and has the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The number of elements that the new list can initially store.</param>
    public AutoSortedList(int capacity) => _internalList = new List<T>(capacity);
    
    /// <summary>
    /// Constructs a new <see cref="AutoSortedList{T}"/> object from another <see cref="List{T}"/>.
    /// </summary>
    /// <param name="list">The list to be wrapped up.</param>
    /// <returns>A new instance of <see cref="AutoSortedList{T}"/>.</returns>
    public static implicit operator AutoSortedList<T>(List<T> list) => new(list);

    // --- Private Methods ---

    /// <summary>
    /// Sorts the elements in the entire internal list using the default comparer.
    /// </summary>
    private void SortList() => _internalList.Sort();

    // --- IEnumerable Implementation ---

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _internalList.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
    public IEnumerator GetEnumerator() => _internalList.GetEnumerator();

    // --- ICollection<T> Implementation ---

    /// <summary>
    /// Gets the number of elements contained in the <see cref="AutoSortedList{T}"/>.
    /// </summary>
    public int Count => _internalList.Count;

    /// <summary>
    /// Gets a value indicating whether the <see cref="AutoSortedList{T}"/> is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an item to the <see cref="AutoSortedList{T}"/> and resorts the list.
    /// </summary>
    /// <param name="item">The object to add to the <see cref="AutoSortedList{T}"/>.</param>
    public void Add(T item)
    {
        _internalList.Add(item);
        SortList();
    }

    /// <summary>
    /// Removes all items from the <see cref="AutoSortedList{T}"/>.
    /// </summary>
    public void Clear() => _internalList.Clear();

    /// <summary>
    /// Determines whether the <see cref="AutoSortedList{T}"/> contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="AutoSortedList{T}"/>.</param>
    /// <returns>true if <paramref name="item"/> is found in the <see cref="AutoSortedList{T}"/>; otherwise, false.</returns>
    public bool Contains(T item) => _internalList.Contains(item);

    /// <summary>
    /// Copies the elements of the <see cref="AutoSortedList{T}"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="AutoSortedList{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
    /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    public void CopyTo(T[] array, int index) => _internalList.CopyTo(array, index);

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="AutoSortedList{T}"/> and resorts the list.
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="AutoSortedList{T}"/>.</param>
    /// <returns>true if <paramref name="item"/> was successfully removed from the <see cref="AutoSortedList{T}"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="AutoSortedList{T}"/>.</returns>
    public bool Remove(T item)
    {
        var removed = _internalList.Remove(item);
        if (removed)
            SortList();
        return removed;
    }

    // --- IList<T> Implementation ---

    /// <summary>
    /// Gets or sets the element at the specified index. Setting a value will trigger a resort.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index]
    {
        get => _internalList[index];
        set
        {
            _internalList[index] = value;
            SortList();
        }
    }

    /// <summary>
    /// Determines the index of a specific item in the <see cref="AutoSortedList{T}"/>.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="AutoSortedList{T}"/>.</param>
    /// <returns>The index of <paramref name="item"/> if found in the list; otherwise, -1.</returns>
    public int IndexOf(T item) => _internalList.IndexOf(item);

    /// <summary>
    /// Inserts an item to the <see cref="AutoSortedList{T}"/> at the specified index and resorts the list.
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
    /// <param name="item">The object to insert into the <see cref="AutoSortedList{T}"/>.</param>
    public void Insert(int index, T item)
    {
        _internalList.Insert(index, item);
        SortList();
    }

    /// <summary>
    /// Removes the <see cref="AutoSortedList{T}"/> item at the specified index. Note: This does not automatically resort as removal by index doesn't change relative order of remaining items, but typically consistent behavior might imply sorting if logic dictates. Here we just remove.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index) => _internalList.RemoveAt(index);
}