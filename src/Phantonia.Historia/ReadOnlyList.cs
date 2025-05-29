using System;
using System.Collections;
using System.Collections.Generic;

namespace Phantonia.Historia;

/// <summary>
/// Represents a read only wrapper around a list.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>This type is mainly used by generated code.</remarks>
public readonly struct ReadOnlyList<T> : IReadOnlyList<T>
{
    /// <summary>
    /// Creates a new read only list from a given list.
    /// </summary>
    /// <param name="list">The backing list.</param>
    public ReadOnlyList(IReadOnlyList<T> list) : this(list, startIndex: 0, list.Count) { }

    /// <summary>
    /// Creates a new read only list from a slice of a given list.
    /// </summary>
    /// <param name="list">The backing list.</param>
    /// <param name="startIndex">The first index to take.</param>
    /// <param name="endIndex">The first index to not take.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ReadOnlyList(IReadOnlyList<T> list, int startIndex, int endIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);

        if (endIndex < startIndex || endIndex > list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(endIndex));
        }

        this.list = list;
        this.startIndex = startIndex;
        this.endIndex = endIndex;
    }

    private readonly IReadOnlyList<T> list;
    private readonly int startIndex;
    private readonly int endIndex;

    public T this[int index] => list[index - startIndex];

    public int Count => endIndex - startIndex;

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            yield return list[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
