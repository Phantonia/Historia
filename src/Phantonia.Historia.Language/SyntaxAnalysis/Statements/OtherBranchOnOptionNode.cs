using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record OtherBranchOnOptionNode : BranchOnOptionNode
{
    public OtherBranchOnOptionNode() { }

    public override IEnumerable<SyntaxNode> Children => [Body];

    protected internal override string GetDebuggerDisplay() => $"option other w/ {Body.Statements.Length} statements";
}
