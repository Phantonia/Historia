using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record SwitchOptionNode : SyntaxNode
{
    public SwitchOptionNode() { }

    public string? Name { get; init; }

    public required ExpressionNode Expression { get; init; }

    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => new SyntaxNode[] { Expression, Body };

    protected internal override string GetDebuggerDisplay() => $"option {Name}{(Name is not null ? " " : "")} w/ {Body.Statements.Length} statements";
}
