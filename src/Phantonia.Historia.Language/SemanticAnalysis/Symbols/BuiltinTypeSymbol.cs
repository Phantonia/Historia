namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record BuiltinTypeSymbol : TypeSymbol
{
    public BuiltinTypeSymbol() { }

    public required BuiltinType Type { get; init; }
}
