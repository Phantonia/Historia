using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record OutputStatementNode : StatementNode, IOutputStatementNode
{
    public OutputStatementNode() { }

    public required ExpressionNode OutputExpression { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { OutputExpression };
}
