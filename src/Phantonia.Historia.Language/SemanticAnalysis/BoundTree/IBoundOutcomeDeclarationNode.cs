using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public interface IBoundOutcomeDeclarationNode : ISyntaxNode
{
    SyntaxNode DeclarationNode { get; }

    OutcomeSymbol Outcome { get; }
}
