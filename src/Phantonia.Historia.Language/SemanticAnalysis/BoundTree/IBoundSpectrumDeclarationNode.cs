using Phantonia.Historia.Language.SemanticAnalysis.Symbols;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public interface IBoundSpectrumDeclarationNode : IBoundOutcomeDeclarationNode
{
    SpectrumSymbol Spectrum { get; }

    OutcomeSymbol IBoundOutcomeDeclarationNode.Outcome => Spectrum;
}
