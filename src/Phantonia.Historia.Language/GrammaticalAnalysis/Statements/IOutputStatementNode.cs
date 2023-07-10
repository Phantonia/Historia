using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public interface IOutputStatementNode
{
    ExpressionNode OutputExpression { get; }
}
