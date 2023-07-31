using Phantonia.Historia.Language.GrammaticalAnalysis;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public interface IBoundOutcomeDeclarationNode : ISyntaxNode
{
    SyntaxNode DeclarationNode { get; }

    OutcomeSymbol Outcome { get; }
}
