namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record BuiltinTypeSymbol() : TypeSymbol
{
    public required BuiltinType Type { get; init; }

    protected internal override string GetDebuggerDisplay() => $"builtin type {Type}";
}
