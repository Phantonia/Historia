﻿using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record OptionNode() : SyntaxNode
{
    public required Token OptionKeywordToken { get; init; }

    public required ExpressionNode Expression { get; init; }

    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Expression, Body];

    protected override void ReconstructCore(TextWriter writer)
    {
        OptionKeywordToken.Reconstruct(writer);
        Expression.Reconstruct(writer);
        Body.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"option ({Expression.GetDebuggerDisplay()}) w/ {Body.Statements.Length} statements";
}
