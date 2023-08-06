using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundSpectrumDeclarationStatementNode : SpectrumDeclarationStatementNode, IBoundSpectrumDeclarationNode
{
    public BoundSpectrumDeclarationStatementNode() { }

    public required SpectrumSymbol Spectrum { get; init; }

    SyntaxNode IBoundOutcomeDeclarationNode.DeclarationNode => this;
}
