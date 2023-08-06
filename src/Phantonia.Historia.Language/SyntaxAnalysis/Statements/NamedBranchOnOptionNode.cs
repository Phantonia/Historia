using System.Collections.Generic;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record NamedBranchOnOptionNode : BranchOnOptionNode
{
    public NamedBranchOnOptionNode() { }

    public required string OptionName { get; init; }
}
