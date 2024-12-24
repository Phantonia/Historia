using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record CallerResolutionStatementNode() : StatementNode
{
    public required CallerTrackerSymbol Tracker { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    internal override void ReconstructCore(TextWriter writer) { }

    protected internal override string GetDebuggerDisplay() => $"resolve tracker for scene {Tracker.CalledScene.Name}";
}
