using System;

namespace Phantonia.Historia;

/// <summary>
/// Represents an outcome that can be set when restoring a chapter.
/// </summary>
/// <typeparam name="T">The enum type associated with the outcome.</typeparam>
public readonly struct CheckpointOutcome<T> where T : unmanaged, Enum
{
    /// <summary>
    /// Represents whether this outcome is required to be set, can optionally be set or is not required at all.
    /// </summary>
    public CheckpointOutcomeKind Kind { get; private init; }

    /// <summary>
    /// Represents the option that this checkpoint outcome has been assigned.
    /// </summary>
    public T Option { get; private init; }

    /// <summary>
    /// Creates a copy of this checkpoint outcome with the given option assigned.
    /// </summary>
    /// <param name="value">The option to assign.</param>
    /// <returns>A copy of this checkpoint outcome.</returns>
    /// <exception cref="ArgumentException"/>
    public CheckpointOutcome<T> Assign(T value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentException($"{value} is not a defined constant of type {typeof(T)}");
        }

        return this with { Option = value };
    }

    /// <summary>
    /// Creates a checkpoint outcome that is not required, i.e. there is no point in assigning it anything.
    /// </summary>
    /// <returns>A checkpoint outcome.</returns>
    public static CheckpointOutcome<T> NotRequired() => new() { Kind = CheckpointOutcomeKind.NotRequired };

    /// <summary>
    /// Creates a checkpoint outcome that is required, i.e. has to be assigned a value that is not Unset.
    /// </summary>
    /// <returns>A checkpoint outcome.</returns>
    public static CheckpointOutcome<T> Required() => new() { Kind = CheckpointOutcomeKind.Required };

    /// <summary>
    /// Creates a checkpoint outcome that can optionally be set.
    /// </summary>
    /// <returns>A checkpoint outcome.</returns>
    public static CheckpointOutcome<T> Optional() => new() { Kind = CheckpointOutcomeKind.Optional };
}
