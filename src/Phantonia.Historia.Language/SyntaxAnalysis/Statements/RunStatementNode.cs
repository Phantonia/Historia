using Phantonia.Historia.Language.LexicalAnalysis;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record RunStatementNode() : MethodCallStatementNode
{
    public required Token SemicolonToken { get; init; }

    internal override void ReconstructCore(TextWriter writer)
    {
        base.ReconstructCore(writer);
        writer.Write(SemicolonToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"run {ReferenceName}.{MethodName}({string.Join(", ", Arguments.Select(a => a.GetDebuggerDisplay()))})";
}
