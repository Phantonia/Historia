using Phantonia.Historia.Language.SyntaxAnalysis.Types;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record PseudoPropertySymbol() : Symbol
{
    public required TypeNode Type { get; init; }

    protected internal override string GetDebuggerDisplay() => $"pseudo property {Name} of type ({Type.GetDebuggerDisplay()})";
}
