namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed record PropertySymbol : Symbol
{
    public PropertySymbol() { }

    public required TypeSymbol Type { get; init; }
}
