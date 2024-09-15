using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public interface IOutputStatementNode : ISyntaxNode
{
    ExpressionNode OutputExpression { get; }

    bool IsCheckpoint { get; }
}
