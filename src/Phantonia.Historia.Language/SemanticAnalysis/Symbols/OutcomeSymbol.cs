using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public record OutcomeSymbol() : Symbol
{
    public required ImmutableArray<string> OptionNames { get; init; }

    public required bool AlwaysAssigned { get; init; }

    public required bool IsPublic { get; init; }

    public string? DefaultOption { get; init; }

    protected internal override string GetDebuggerDisplay() => $"outcome symbol {Name} w/ options ({string.Join(", ", OptionNames)}) {(DefaultOption is not null ? "default " : "")}{DefaultOption}";
}
