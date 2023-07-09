using System.Collections.Immutable;
using Phantonia.Historia.Language.GrammaticalAnalysis;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record StatementBodyNode : SyntaxNode
{
    public StatementBodyNode() { }

    public ImmutableArray<StatementNode> Statements { get; init; }
}
