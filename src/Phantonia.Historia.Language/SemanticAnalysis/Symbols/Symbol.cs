namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public abstract record Symbol
{
    protected Symbol() { }

    public required int Index { get; init; }

    public required string Name { get; init; }
}
