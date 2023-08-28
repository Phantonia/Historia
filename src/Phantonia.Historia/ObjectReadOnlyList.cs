using System.Collections;
using System.Collections.Generic;

namespace Phantonia.Historia;

public readonly struct ObjectReadOnlyList<T> : IReadOnlyList<object?>
{
    public ObjectReadOnlyList(ReadOnlyList<T> list)
    {
        this.list = list;
    }

    private readonly ReadOnlyList<T> list;

    public object? this[int index] => list[index];

    public int Count { get; }

    public IEnumerator<object?> GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
        {
            yield return list[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
