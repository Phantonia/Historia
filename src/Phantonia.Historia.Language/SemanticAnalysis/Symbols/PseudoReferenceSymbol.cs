namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record PseudoReferenceSymbol() : Symbol
{
    public required string InterfaceName { get; init; }

    protected internal override string GetDebuggerDisplay() => $"(pseudo) reference {Name}: {InterfaceName}";
}
