using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public record InterfaceMethodDeclarationNode() : SyntaxNode
{
    public required Token InterfaceKeyword { get; init; }

    public required Token KindToken { get; init; }

    public InterfaceMethodKind Kind => (InterfaceMethodKind)KindToken.Kind;

    public required Token NameToken { get; init; }

    public string Name => NameToken.Text;

    public required Token OpenParenthesisToken { get; init; }

    public required ImmutableArray<ParameterDeclarationNode> Parameters { get; init; }

    public required Token ClosedParenthesisToken { get; init; }

    public required Token? CommaToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => Parameters;

    protected override void ReconstructCore(TextWriter writer)
    {
        InterfaceKeyword.Reconstruct(writer);
        KindToken.Reconstruct(writer);
        NameToken.Reconstruct(writer);
        OpenParenthesisToken.Reconstruct(writer);

        foreach (ParameterDeclarationNode parameter in Parameters)
        {
            parameter.Reconstruct(writer);
        }

        ClosedParenthesisToken.Reconstruct(writer);
        CommaToken?.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"{(Kind == InterfaceMethodKind.Action ? "action" : "choice")} {Name}({string.Join(", ", Parameters.Select(p => p.GetDebuggerDisplay()))}";
}
