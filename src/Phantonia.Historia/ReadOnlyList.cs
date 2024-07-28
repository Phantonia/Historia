using System;
using System.Collections;
using System.Collections.Generic;

namespace Phantonia.Historia;

public readonly struct ReadOnlyList<T> : IReadOnlyList<T>
{
    public ReadOnlyList(IReadOnlyList<T> list) : this(list, startIndex: 0, list.Count) { }

    public ReadOnlyList(IReadOnlyList<T> list, int startIndex, int endIndex)
    {
        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

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
