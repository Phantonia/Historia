﻿using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record SpectrumAdjustmentStatementNode() : StatementNode
{
    public required bool Strengthens { get; init; }

    public bool Weakens => !Strengthens;

    public required string SpectrumName { get; init; }

    public required ExpressionNode AdjustmentAmount { get; init; }

    public override IEnumerable<SyntaxNode> Children => [AdjustmentAmount];

    protected internal override string GetDebuggerDisplay() => $"{(Strengthens ? "strengthen" : "weaken")} {SpectrumName} by {AdjustmentAmount.GetDebuggerDisplay()}";
}
