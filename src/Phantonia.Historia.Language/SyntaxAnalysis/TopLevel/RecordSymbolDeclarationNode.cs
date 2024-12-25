using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record RecordSymbolDeclarationNode() : TypeSymbolDeclarationNode
{
    public required Token RecordKeywordToken { get; init; }

    public required Token OpenParenthesisToken { get; init; }

    public required ImmutableArray<ParameterDeclarationNode> Properties { get; init; }

    public required Token ClosedParenthesisToken { get; init; }

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => Properties;

    protected override void ReconstructCore(TextWriter writer)
    {
        RecordKeywordToken.Reconstruct(writer);
        NameToken.Reconstruct(writer);
        OpenParenthesisToken.Reconstruct(writer);

        foreach (ParameterDeclarationNode property in Properties)
        {
            property.Reconstruct(writer);
        }

        ClosedParenthesisToken.Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"declare record {Name} {{ {string.Join(", ", Properties.Select(p => p.GetDebuggerDisplay()))} }}";
}
