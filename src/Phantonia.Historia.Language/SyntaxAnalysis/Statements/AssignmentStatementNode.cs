using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record AssignmentStatementNode() : StatementNode
{
    public required Token VariableNameToken { get; init; }

    public string VariableName => VariableNameToken.Text;

    public required Token EqualsSignToken { get; init; }

    public required ExpressionNode AssignedExpression { get; init; }

    public override IEnumerable<SyntaxNode> Children => [AssignedExpression];

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(VariableNameToken.Reconstruct());
        writer.Write(EqualsSignToken.Reconstruct());
        AssignedExpression.Reconstruct();
    }

    protected internal override string GetDebuggerDisplay() => $"assignment {VariableName} = {AssignedExpression.GetDebuggerDisplay()}";
}
