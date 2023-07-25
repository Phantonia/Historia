using System.Collections.Generic;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record StatementBodyNode : SyntaxNode
{
    public StatementBodyNode() { }

    public required ImmutableArray<StatementNode> Statements { get; init; }

    public override IEnumerable<SyntaxNode> Children => Statements;
}
