using Phantonia.Historia.Language.SemanticAnalysis.Symbols;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record CallerTrackerSymbol : Symbol
{
    public CallerTrackerSymbol() { }

    public required SceneSymbol CalledScene { get; init; }

    public required int CallSiteCount { get; init; }

    protected internal override string GetDebuggerDisplay() => $"tracker for scene {CalledScene.Name} w/ {CallSiteCount} callsites";
}
