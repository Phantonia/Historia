using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public abstract record BranchOnOptionNode() : SyntaxNode
{
    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Body];
}
