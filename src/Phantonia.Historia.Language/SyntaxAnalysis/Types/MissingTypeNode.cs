using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Types;

public sealed record MissingTypeNode() : TypeNode
{
    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer) { }

    protected internal override string GetDebuggerDisplay() => "<missing type>";
}
