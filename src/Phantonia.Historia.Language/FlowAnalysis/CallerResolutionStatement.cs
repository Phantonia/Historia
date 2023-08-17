using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record CallerResolutionStatement : StatementNode
{
    public CallerResolutionStatement() { }

    public required CallerTrackerSymbol Tracker { get; init; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();

    protected internal override string GetDebuggerDisplay() => $"resolve tracker for scene {Tracker.CalledScene.Name}";
}
