namespace Phantonia.Historia.Language.SemanticAnalysis;

public readonly struct BindingContext
{
    public SymbolTable SymbolTable { get; init; }

    public bool IsInLoopSwitch { get; init; }

    public bool IsInScene { get; init; }
}
