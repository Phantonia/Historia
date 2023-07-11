namespace Phantonia.Historia.Language.GrammaticalAnalysis.Types;

public sealed record IdentifierTypeNode : TypeNode
{
    public IdentifierTypeNode() { }

    public required string Identifier { get; init; }
}
