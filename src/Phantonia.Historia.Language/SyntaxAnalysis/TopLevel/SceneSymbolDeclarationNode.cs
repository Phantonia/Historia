using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record SceneSymbolDeclarationNode() : SymbolDeclarationNode
{
    public required Token SceneOrChapterKeywordToken { get; init; }

    public bool IsChapter => SceneOrChapterKeywordToken.Kind is TokenKind.ChapterKeyword;

    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Body];

    protected override void ReconstructCore(TextWriter writer)
    {
        SceneOrChapterKeywordToken.Reconstruct(writer);
        NameToken.Reconstruct(writer);
        Body.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"scene {Name} w/ {Body.Statements.Length} statements";
}
