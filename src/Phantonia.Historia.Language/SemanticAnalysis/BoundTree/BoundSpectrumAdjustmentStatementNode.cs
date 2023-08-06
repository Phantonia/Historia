using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundSpectrumAdjustmentStatementNode : SpectrumAdjustmentStatementNode
{
    public BoundSpectrumAdjustmentStatementNode() { }

    public required SpectrumSymbol Spectrum { get; init; }
}
