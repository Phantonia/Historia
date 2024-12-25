using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record CallStatementNode() : StatementNode
{
    public required Token CallKeywordToken { get; init; }

    public required Token SceneNameToken { get; init; }

    public string SceneName => SceneNameToken.Text;

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer)
    {
        CallKeywordToken.Reconstruct(writer);
        SceneNameToken.Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"call {SceneName}";
}
