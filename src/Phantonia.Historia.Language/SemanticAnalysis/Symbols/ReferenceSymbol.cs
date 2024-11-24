namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record ReferenceSymbol() : Symbol
{
    public required InterfaceSymbol Interface { get; init; }

    protected internal override string GetDebuggerDisplay() => $"reference {Name}: {Interface.Name}";
}
