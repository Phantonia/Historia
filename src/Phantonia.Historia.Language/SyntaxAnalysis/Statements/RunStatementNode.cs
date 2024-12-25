using Phantonia.Historia.Language.LexicalAnalysis;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record RunStatementNode() : MethodCallStatementNode
{
    public required Token SemicolonToken { get; init; }

    protected override void ReconstructCore(TextWriter writer)
    {
        base.ReconstructCore(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"run {ReferenceName}.{MethodName}({string.Join(", ", Arguments.Select(a => a.GetDebuggerDisplay()))})";
}
