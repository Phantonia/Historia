using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public interface IOptionsStatementNode : IOutputStatementNode
{
    IEnumerable<ExpressionNode> OptionExpressions { get; }
}
