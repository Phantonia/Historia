namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public sealed record NamedBranchOnOptionNode : BranchOnOptionNode
{
    public NamedBranchOnOptionNode() { }

    public required string OptionName { get; init; }
}
