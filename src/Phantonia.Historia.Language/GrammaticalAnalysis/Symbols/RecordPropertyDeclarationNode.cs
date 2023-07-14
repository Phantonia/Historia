using Phantonia.Historia.Language.GrammaticalAnalysis.Types;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Symbols;

public sealed record RecordPropertyDeclarationNode : SyntaxNode
{
    public RecordPropertyDeclarationNode() { }

    public required string Name { get; init; }

    public required TypeNode Type { get; init; }
}
