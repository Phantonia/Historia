using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public interface IBranchingStatementNode : ISyntaxNode
{
    IEnumerable<StatementBodyNode> Bodies { get; }
}
