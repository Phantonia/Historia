using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public abstract record BranchOnOptionNode() : SyntaxNode
{
    public required Token OptionKeywordToken { get; init; }

    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Body];
}
