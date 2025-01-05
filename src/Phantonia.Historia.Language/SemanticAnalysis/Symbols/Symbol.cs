using System.Diagnostics;

namespace Phantonia.Historia.Language.SemanticAnalysis.Symbols;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public abstract record Symbol
{
    protected Symbol() { }

    public required long Index { get; init; }

    public required string Name { get; init; }

    protected internal abstract string GetDebuggerDisplay();
}
