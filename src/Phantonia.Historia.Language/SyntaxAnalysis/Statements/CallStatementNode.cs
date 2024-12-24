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

    internal override string ReconstructCore() => CallKeywordToken.Reconstruct() + SceneNameToken.Reconstruct() + SemicolonToken.Reconstruct();

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(CallKeywordToken.Reconstruct());
        writer.Write(SceneNameToken.Reconstruct());
        writer.Write(SemicolonToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"call {SceneName}";
}
