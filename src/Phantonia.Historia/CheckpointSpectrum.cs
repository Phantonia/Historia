namespace Phantonia.Historia;

public readonly struct CheckpointSpectrum
{
    public CheckpointOutcomeKind Kind { get; private init; }

    public int PositiveCount { get; private init; }

    public int TotalCount { get; private init; }

    public CheckpointSpectrum Assign(int positiveCount, int totalCount) => this with
    {
        PositiveCount = positiveCount,
        TotalCount = totalCount,
    };
    
    public static CheckpointSpectrum NotRequired() => new() { Kind = CheckpointOutcomeKind.NotRequired };

    public static CheckpointSpectrum Required() => new() { Kind = CheckpointOutcomeKind.Required };

    public static CheckpointSpectrum Optional() => new() { Kind = CheckpointOutcomeKind.Optional };
}
