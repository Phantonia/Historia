namespace Phantonia.Historia.Language.SemanticAnalysis;

public abstract record Symbol
{
    protected Symbol() { }

    public required string Name { get; init; }
}
