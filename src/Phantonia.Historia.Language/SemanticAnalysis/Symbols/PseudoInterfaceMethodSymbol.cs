using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record PseudoInterfaceMethodSymbol() : Symbol
{
    public required InterfaceMethodKind Kind { get; init; }

    public required ImmutableArray<PseudoPropertySymbol> Parameters { get; init; }

    protected internal override string GetDebuggerDisplay() => $"{(Kind == InterfaceMethodKind.Action ? "action" : "choice")} {Name}({string.Join(", ", Parameters.Select(p => p.GetDebuggerDisplay()))}";
}
