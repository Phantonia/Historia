using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record CallerResolutionStatementNode : StatementNode
{
    public CallerResolutionStatementNode() { }

    public required CallerTrackerSymbol Tracker { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected internal override string GetDebuggerDisplay() => $"resolve tracker for scene {Tracker.CalledScene.Name}";
}
