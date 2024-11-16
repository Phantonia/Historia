namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record SwitchOptionNode : OptionNode
{
    public SwitchOptionNode() { }

    public string? Name { get; init; }

    protected internal override string GetDebuggerDisplay() => $"option {Name}{(Name is not null ? " " : "")} ({Expression.GetDebuggerDisplay()}) w/ {Body.Statements.Length} statements";
}
