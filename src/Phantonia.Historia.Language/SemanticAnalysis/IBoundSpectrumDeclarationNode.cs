namespace Phantonia.Historia.Language.SemanticAnalysis;

public interface IBoundSpectrumDeclarationNode : IBoundOutcomeDeclarationNode
{
    SpectrumSymbol Spectrum { get; }

    OutcomeSymbol IBoundOutcomeDeclarationNode.Outcome => Spectrum;
}
