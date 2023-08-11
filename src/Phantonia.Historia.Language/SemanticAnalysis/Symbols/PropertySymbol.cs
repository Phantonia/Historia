namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record PropertySymbol : Symbol
{
    public PropertySymbol() { }

    public required TypeSymbol Type { get; init; }

    protected internal override string GetDebuggerDisplay() => $"property {Name} of type ({Type.GetDebuggerDisplay()})";
}
