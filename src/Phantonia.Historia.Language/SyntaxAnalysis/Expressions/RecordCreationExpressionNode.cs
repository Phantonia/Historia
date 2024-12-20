using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public sealed record RecordCreationExpressionNode() : ExpressionNode, IArgumentContainerNode
{
    public required Token RecordNameToken { get; init; }

    public string RecordName => RecordNameToken.Text;

    public required Token OpenParenthesisToken { get; init; }

    public required ImmutableArray<ArgumentNode> Arguments { get; init; }

    public required Token ClosedParenthesisToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => Arguments;

    internal override string ReconstructCore()
    {
        StringWriter writer = new();
        ReconstructCore(writer);
        return writer.ToString();
    }

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(RecordNameToken.Reconstruct());
        writer.Write(OpenParenthesisToken.Reconstruct());

        foreach (ArgumentNode argument in Arguments)
        {
            argument.Reconstruct(writer);
        }

        writer.Write(ClosedParenthesisToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"creation {RecordName}({string.Join(", ", Arguments.Select(a => a.GetDebuggerDisplay()))})";
}
