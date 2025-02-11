using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundLineStatementNode() : StatementNode, IOutputStatementNode
{
    public required LineStatementNode LineStatement { get; init; }

    public required TypedExpressionNode LineCreationExpression { get; init; }

    public override IEnumerable<SyntaxNode> Children => [LineStatement];

    protected override void ReconstructCore(TextWriter writer) => LineStatement.Reconstruct(writer);

    protected internal override string GetDebuggerDisplay() => $"{LineStatement.GetDebuggerDisplay()} bound @ {((BoundRecordCreationExpressionNode)LineCreationExpression.Original).Record.GetDebuggerDisplay()}";

    ExpressionNode IOutputStatementNode.OutputExpression => LineCreationExpression;
}
