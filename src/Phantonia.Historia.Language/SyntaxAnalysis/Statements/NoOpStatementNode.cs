using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record NoOpStatementNode() : StatementNode
{
    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer) { }

    protected internal override string GetDebuggerDisplay() => "noop";
}
