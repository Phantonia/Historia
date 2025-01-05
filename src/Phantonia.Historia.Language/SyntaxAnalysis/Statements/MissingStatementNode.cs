using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record MissingStatementNode() : StatementNode
{
    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer) { }

    protected internal override string GetDebuggerDisplay() => "<missing statement>";
}
