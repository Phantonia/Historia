﻿using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public record ArgumentNode : SyntaxNode
{
    public ArgumentNode() { }

    public required ExpressionNode Expression { get; init; }

    public string? PropertyName { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { Expression };

    protected internal override string GetDebuggerDisplay() => $"argument {Expression.GetDebuggerDisplay()}";
}
