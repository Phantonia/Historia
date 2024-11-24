using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record RunStatementNode() : MethodCallStatementNode
{
    protected internal override string GetDebuggerDisplay() => $"run {ReferenceName}.{MethodName}({string.Join(", ", Arguments.Select(a => a.GetDebuggerDisplay()))})";
}
