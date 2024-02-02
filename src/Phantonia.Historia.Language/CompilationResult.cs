using System.Collections.Immutable;

namespace Phantonia.Historia.Language;

public readonly record struct CompilationResult
{
    public CompilationResult() { }

    public string? StoryName { get; init; } = null;

    public ImmutableArray<Error> Errors { get; init; } = ImmutableArray<Error>.Empty;

    public bool IsValid => Errors.Length == 0;
}
