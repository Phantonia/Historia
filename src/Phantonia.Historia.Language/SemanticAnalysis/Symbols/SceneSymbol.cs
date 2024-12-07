namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

public sealed record SceneSymbol() : Symbol
{
    protected internal override string GetDebuggerDisplay() => $"scene symbol {Name}";
}
