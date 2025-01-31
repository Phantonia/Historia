using Phantonia.Historia.Language.SemanticAnalysis.Symbols;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record CallerTrackerSymbol : Symbol
{
    public CallerTrackerSymbol() { }

    public required SubroutineSymbol CalledSubroutine { get; init; }

    public required int CallSiteCount { get; init; }

    protected internal override string GetDebuggerDisplay() => $"tracker for scene {CalledSubroutine.Name} w/ {CallSiteCount} callsites";
}
