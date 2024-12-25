using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public sealed record ReferenceSymbolDeclarationNode() : SymbolDeclarationNode
{
    public required Token ReferenceKeywordToken { get; init; }

    public required Token ColonToken { get; init; }

    public required Token InterfaceNameToken { get; init; }

    public string InterfaceName => InterfaceNameToken.Text;

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer)
    {
        ReferenceKeywordToken.Reconstruct(writer);
        NameToken.Reconstruct(writer);
        ColonToken.Reconstruct(writer);
        InterfaceNameToken.Reconstruct(writer);
        SemicolonToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"reference {Name}: {InterfaceName}";
}
