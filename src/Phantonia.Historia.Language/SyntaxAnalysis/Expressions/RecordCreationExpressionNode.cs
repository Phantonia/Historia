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

    public override bool IsConstant => Arguments.All(a => a.Expression.IsConstant);

    public override IEnumerable<SyntaxNode> Children => Arguments;

    protected override void ReconstructCore(TextWriter writer)
    {
        RecordNameToken.Reconstruct(writer);
        OpenParenthesisToken.Reconstruct(writer);

        foreach (ArgumentNode argument in Arguments)
        {
            argument.Reconstruct(writer);
        }

        ClosedParenthesisToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"creation {RecordName}({string.Join(", ", Arguments.Select(a => a.GetDebuggerDisplay()))})";
}
