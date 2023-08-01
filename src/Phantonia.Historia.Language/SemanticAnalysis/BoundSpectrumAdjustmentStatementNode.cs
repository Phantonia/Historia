using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundSpectrumAdjustmentStatementNode : SpectrumAdjustmentStatementNode
{
    public BoundSpectrumAdjustmentStatementNode() { }

    public required SpectrumSymbol Spectrum { get; init; }
}
