using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record OutcomeDeclarationStatementNode : StatementNode, IOutcomeDeclarationNode
{
    public OutcomeDeclarationStatementNode() { }

    public required string Name { get; init; }

    public required ImmutableArray<string> Options { get; init; }

    public string? DefaultOption { get; init; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();
}
