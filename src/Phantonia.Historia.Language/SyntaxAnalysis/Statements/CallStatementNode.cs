using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record CallStatementNode : StatementNode
{
    public CallStatementNode() { }

    public required string SceneName { get; init; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();

    protected internal override string GetDebuggerDisplay() => $"call {SceneName}";
}
