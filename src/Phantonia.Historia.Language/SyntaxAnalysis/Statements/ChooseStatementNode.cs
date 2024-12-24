using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record ChooseStatementNode() : MethodCallStatementNode
{
    public required Token OpenBraceToken { get; init; }

    public required ImmutableArray<OptionNode> Options { get; init; }

    public required Token ClosedBraceToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [.. base.Children, .. Options];

    internal override void ReconstructCore(TextWriter writer)
    {
        base.ReconstructCore(writer);
        writer.Write(OpenBraceToken.Reconstruct());

        foreach (OptionNode option in Options)
        {
            option.Reconstruct(writer);
        }

        writer.Write(ClosedBraceToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay()
        => $"choose {ReferenceName}.{MethodName}({string.Join(", ", Arguments.Select(a => a.GetDebuggerDisplay()))}) {{ {string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))} }}";
}
