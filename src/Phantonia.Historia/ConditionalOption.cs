namespace Phantonia.Historia;

public readonly struct ConditionalOption<T>
{
    public T Option { get; }

    public bool IsAvailable { get; }
}
