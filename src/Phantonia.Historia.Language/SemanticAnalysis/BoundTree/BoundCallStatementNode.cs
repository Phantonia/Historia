using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;

namespace Phantonia.Historia.Language.SemanticAnalysis.BoundTree;

public sealed record BoundCallStatementNode() : CallStatementNode
{
    public required SubroutineSymbol Subroutine { get; init; }
}
