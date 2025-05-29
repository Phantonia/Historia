namespace Phantonia.Historia;

/// <summary>
/// Represents the kind of a <see cref="CheckpointOutcome{T}"/> or <see cref="CheckpointSpectrum"/>.
/// </summary>
public enum CheckpointOutcomeKind
{
    /// <summary>
    /// There is no point setting this outcome.
    /// </summary>
    NotRequired,

    /// <summary>
    /// It is required to set this outcome.
    /// </summary>
    Required,
    
    /// <summary>
    /// It is optional to set this outcome.
    /// </summary>
    Optional,
}
