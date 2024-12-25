using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public record ParameterDeclarationNode() : SyntaxNode
{
    public required Token NameToken { get; init; }

    public string Name => NameToken.Text;

    public required Token ColonToken { get; init; }

    public required TypeNode Type { get; init; }

    public required Token? CommaToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => [Type];

    protected override void ReconstructCore(TextWriter writer)
    {
        NameToken.Reconstruct(writer);
        ColonToken.Reconstruct(writer);
        Type.Reconstruct(writer);
        CommaToken?.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"property {Name} of type {Type.GetDebuggerDisplay()}";
}
