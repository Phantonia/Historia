using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record OutcomeDeclarationStatementNode() : StatementNode, IOutcomeDeclarationNode
{
    public required string Name { get; init; }

    public required ImmutableArray<string> Options { get; init; }

    public required string? DefaultOption { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"declare outcome {Name} ({string.Join(", ", Options)}) {(DefaultOption is not null ? "default " : "")}{DefaultOption}";

    bool IOutcomeDeclarationNode.Public => false;
}
