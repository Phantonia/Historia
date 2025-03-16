namespace Phantonia.Historia;

/// <summary>
/// Represents an option that might not be available.
/// </summary>
/// <typeparam name="T">The option type.</typeparam>
public readonly struct ConditionalOption<T>
{
    /// <summary>
    /// The option value.
    /// </summary>
    public T Option { get; }

    /// <summary>
    /// Whether this option is available.
    /// </summary>
    public bool IsAvailable { get; }
}
