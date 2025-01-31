using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record SubroutineSymbolDeclarationNode() : SymbolDeclarationNode
{
    public required Token DeclaratorToken { get; init; }

    public SubroutineKind Kind => (SubroutineKind)DeclaratorToken.Kind;

    public bool IsChapter => Kind is SubroutineKind.Chapter;

    public bool IsScene => Kind is SubroutineKind.Scene;

    public required StatementBodyNode Body { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Body];

    protected override void ReconstructCore(TextWriter writer)
    {
        DeclaratorToken.Reconstruct(writer);
        NameToken.Reconstruct(writer);
        Body.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"scene {Name} w/ {Body.Statements.Length} statements";
}
