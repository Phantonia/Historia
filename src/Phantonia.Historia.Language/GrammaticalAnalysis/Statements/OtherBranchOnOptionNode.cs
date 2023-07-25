using System.Collections.Generic;

namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record OtherBranchOnOptionNode : BranchOnOptionNode
{
    public OtherBranchOnOptionNode() { }

    public override IEnumerable<SyntaxNode> Children => new[] { Body };
}
