namespace Phantonia.Historia.Language.GrammaticalAnalysis.Statements;

public abstract record BranchOnOptionNode : SyntaxNode
{
    protected BranchOnOptionNode() { }

    public required StatementBodyNode Body { get; init; }
}
