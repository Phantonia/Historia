using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public record SpectrumAdjustmentStatementNode : StatementNode
{
    public SpectrumAdjustmentStatementNode() { }

    public required bool Strengthens { get; init; }

    public bool Weakens => !Strengthens;

    public required string SpectrumName { get; init; }

    public required ExpressionNode AdjustmentAmount { get; init; }

    public override IEnumerable<SyntaxNode> Children => new[] { AdjustmentAmount };
}
