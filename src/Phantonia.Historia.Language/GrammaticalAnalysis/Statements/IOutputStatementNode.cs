using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public interface IOutputStatementNode : ISyntaxNode
{
    ExpressionNode OutputExpression { get; }
}
