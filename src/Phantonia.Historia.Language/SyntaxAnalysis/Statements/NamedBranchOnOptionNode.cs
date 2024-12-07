namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record NamedBranchOnOptionNode() : BranchOnOptionNode
{
    public required string OptionName { get; init; }

    protected internal override string GetDebuggerDisplay() => $"option {OptionName} w/ {Body.Statements.Length} statements";
}
