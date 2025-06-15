using System;

namespace Phantonia.Historia;

/// <summary>
/// Represents a spectrum that can get a value when restoring a chapter.
/// </summary>
public readonly struct CheckpointSpectrum
{
    /// <summary>
    /// The kind of this checkpoint spectrum.
    /// </summary>
    public CheckpointOutcomeKind Kind { get; private init; }

    /// <summary>
    /// The amount of positive interactions.
    /// </summary>
    public uint PositiveCount { get; private init; }

    /// <summary>
    /// The total amount of interactions.
    /// </summary>
    public uint TotalCount { get; private init; }

    /// <summary>
    /// Creates a copy of this checkpoint outcome with the given fraction assigned.
    /// </summary>
    /// <param name="positiveCount">The amount of positive interactions.</param>
    /// <param name="totalCount">The total amount of interactions.</param>
    /// <returns>A copy of this checkpoint spectrum.</returns>
    public CheckpointSpectrum Assign(uint positiveCount, uint totalCount)
    {
        if (positiveCount < 0 || totalCount < 0 || totalCount < positiveCount)
        {
            throw new ArgumentException($"0 <= {nameof(positiveCount)} <= {nameof(totalCount)} has to hold");
        }

        return this with
        {
            PositiveCount = positiveCount,
            TotalCount = totalCount,
        };
    }

    /// <summary>
    /// Creates a checkpoint spectrum that is not required, i.e. there is no point in assigning it anything.
    /// </summary>
    /// <returns>A checkpoint spectrum.</returns>
    public static CheckpointSpectrum NotRequired() => new() { Kind = CheckpointOutcomeKind.NotRequired };

    /// <summary>
    /// Creates a checkpoint spectrum that is required, i.e. has to be assigned a value that is not Unset.
    /// </summary>
    /// <returns>A checkpoint spectrum.</returns>
    public static CheckpointSpectrum Required() => new() { Kind = CheckpointOutcomeKind.Required };

    /// <summary>
    /// Creates a checkpoint spectrum that can optionally be set.
    /// </summary>
    /// <returns>A checkpoint spectrum.</returns>
    public static CheckpointSpectrum Optional() => new() { Kind = CheckpointOutcomeKind.Optional };
}
