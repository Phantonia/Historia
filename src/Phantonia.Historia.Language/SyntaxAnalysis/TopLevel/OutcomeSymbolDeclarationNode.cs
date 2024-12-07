using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record OutcomeSymbolDeclarationNode() : SymbolDeclarationNode, IOutcomeDeclarationNode
{
    public required bool Public { get; init; }

    public required ImmutableArray<string> Options { get; init; }

    public required string? DefaultOption { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"declare outcome {Name} ({string.Join(", ", Options)}) {(DefaultOption is not null ? "default " : "")}{DefaultOption}";
}
