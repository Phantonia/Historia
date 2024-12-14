using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public record InterfaceMethodDeclarationNode() : SyntaxNode
{
    public required Token InterfaceKeyword { get; init; }

    public required InterfaceMethodKind Kind { get; init; }

    public required string Name { get; init; }

    public required ImmutableArray<PropertyDeclarationNode> Parameters { get; init; }

    public override IEnumerable<SyntaxNode> Children => Parameters;

    protected internal override string GetDebuggerDisplay() => $"{(Kind == InterfaceMethodKind.Action ? "action" : "choice")} {Name}({string.Join(", ", Parameters.Select(p => p.GetDebuggerDisplay()))}";
}
