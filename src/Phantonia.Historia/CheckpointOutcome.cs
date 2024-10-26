using System;

namespace Phantonia.Historia;

public readonly struct CheckpointOutcome<T> where T : unmanaged, Enum
{
    public CheckpointOutcomeKind Kind { get; private init; }

    public T Option { get; private init; }

    public CheckpointOutcome<T> Assign(T value) => this with { Option = value };

    public static CheckpointOutcome<T> NotRequired() => new() { Kind = CheckpointOutcomeKind.NotRequired };

    public static CheckpointOutcome<T> Required() => new() { Kind = CheckpointOutcomeKind.Required };

    public static CheckpointOutcome<T> Optional() => new() { Kind = CheckpointOutcomeKind.Optional };
}
