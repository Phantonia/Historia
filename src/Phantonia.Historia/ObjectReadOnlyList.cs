using System.Collections;
using System.Collections.Generic;

namespace Phantonia.Historia;

/// <summary>
/// Represents a list of objects that can only be read from a generic <see cref="ReadOnlyList{T}"/>.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <param name="list">The backing list.</param>
/// <remarks>This type is mainly used by generated code.</remarks>
public readonly struct ObjectReadOnlyList<T>(ReadOnlyList<T> list) : IReadOnlyList<object?>
{
    public object? this[int index] => list[index];

    public int Count => list.Count;

    public IEnumerator<object?> GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
        {
            yield return list[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
