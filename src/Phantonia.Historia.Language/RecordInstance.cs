using System.Collections.Immutable;

namespace Phantonia.Historia.Language;

public sealed record RecordInstance
{
    public required string RecordName { get; init; }

    public required ImmutableDictionary<string, object> Properties { get; init; }
}
