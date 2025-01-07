using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public abstract record MethodCallStatementNode() : StatementNode, IArgumentContainerNode
{
    public required Token RunOrChooseKeywordToken { get; init; }

    public required Token ReferenceNameToken { get; init; }

    public string ReferenceName => ReferenceNameToken.Text;

    public required Token DotToken { get; init; }

    public required Token MethodNameToken { get; init; }

    public string MethodName => MethodNameToken.Text;

    public required Token OpenParenthesisToken { get; init; }

    public required ImmutableArray<ArgumentNode> Arguments { get; init; }

    public required Token ClosedParenthesisToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => Arguments;

    protected override void ReconstructCore(TextWriter writer)
    {
        RunOrChooseKeywordToken.Reconstruct(writer);
        ReferenceNameToken.Reconstruct(writer);
        DotToken.Reconstruct(writer);
        MethodNameToken.Reconstruct(writer);
        OpenParenthesisToken.Reconstruct(writer);

        foreach (ArgumentNode argument in Arguments)
        {
            argument.Reconstruct(writer);
        }

        ClosedParenthesisToken.Reconstruct(writer);
    }
}
