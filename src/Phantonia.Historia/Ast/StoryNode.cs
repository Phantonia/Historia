using Phantonia.Historia.Language.Ast.Symbols;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.Ast;

public sealed record StoryNode
{
    public StoryNode() { }

    public required ImmutableArray<SymbolDeclarationNode> Symbols { get; init; }
}
