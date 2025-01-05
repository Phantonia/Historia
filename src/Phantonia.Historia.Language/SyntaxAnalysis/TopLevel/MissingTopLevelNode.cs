using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record MissingTopLevelNode() : TopLevelNode
{
    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer) { }

    protected internal override string GetDebuggerDisplay() => "<missing top level node>";
}
