using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.FlowAnalysis;

public sealed record CallerResolutionStatement : StatementNode
{
    public CallerResolutionStatement() { }

    public required CallerTrackerSymbol Tracker { get; init; }

    public required ImmutableArray<int> CallSites { get; init; }

    public override IEnumerable<SyntaxNode> Children => Enumerable.Empty<SyntaxNode>();
}
