using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record CallerTrackerStatementNode() : StatementNode
{
    public required CallerTrackerSymbol Tracker { get; init; }

    public required int CallSiteIndex { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    internal override void ReconstructCore(TextWriter writer) { }

    protected internal override string GetDebuggerDisplay() => $"track scene {Tracker.CalledScene.Name} @ callsite {CallSiteIndex}";
}
