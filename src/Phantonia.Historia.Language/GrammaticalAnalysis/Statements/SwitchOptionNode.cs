using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record SwitchOptionNode : SyntaxNode
{
    public SwitchOptionNode() { }

    public string? Name { get; init; }

    public required ExpressionNode Expression { get; init; }

    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => new SyntaxNode[] { Expression, Body };
}
