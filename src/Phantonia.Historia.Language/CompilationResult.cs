using System.Collections.Immutable;

namespace Phantonia.Historia.Language;

public readonly record struct CompilationResult
{
    public CompilationResult() { }

    public ImmutableArray<Error> Errors { get; init; } = [];

    public required LineIndexing LineIndexing { get; init; }

    public required ulong Fingerprint { get; init; }

    public bool IsValid => Errors.Length == 0;
}
