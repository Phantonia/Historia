using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record BoundSpectrumDeclarationStatementNode : SpectrumDeclarationStatementNode, IBoundSpectrumDeclarationNode
{
    public BoundSpectrumDeclarationStatementNode() { }

    public required SpectrumSymbol Spectrum { get; init; }

    SyntaxNode IBoundOutcomeDeclarationNode.DeclarationNode => this;
}
