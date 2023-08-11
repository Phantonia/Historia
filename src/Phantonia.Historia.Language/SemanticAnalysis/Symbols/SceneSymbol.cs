namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record SceneSymbol : Symbol
{
    public SceneSymbol() { }

    protected internal override string GetDebuggerDisplay() => $"scene symbol {Name}";
}
