using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record SpectrumDeclarationStatementNode() : StatementNode, ISpectrumDeclarationNode
{
    public required Token SpectrumKeywordToken { get; init; }

    public required Token NameToken { get; init; }

    public string Name => NameToken.Text;

    public required Token OpenParenthesisToken { get; init; }

    private readonly ImmutableArray<SpectrumOptionNode> options;
    private readonly ImmutableArray<string> stringOptions;

    public required ImmutableArray<SpectrumOptionNode> Options
    {
        get => options;
        init
        {
            options = value;
            stringOptions = value.Select(o => o.Name).ToImmutableArray();
        }
    }

    public required Token ClosedParenthesisToken { get; init; }

    public required Token? DefaultKeywordToken { get; init; }

    public required Token? DefaultOptionToken { get; init; }

    public string? DefaultOption => DefaultOptionToken?.Text;

    public required Token SemicolonToken { get; init; }

    public override IEnumerable<SyntaxNode> Children => Options;

    ImmutableArray<string> IOutcomeDeclarationNode.Options => stringOptions;

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(SpectrumKeywordToken.Reconstruct());
        writer.Write(NameToken.Reconstruct());
        writer.Write(OpenParenthesisToken.Reconstruct());

        foreach (SpectrumOptionNode option in Options)
        {
            option.Reconstruct(writer);
        }

        writer.Write(ClosedParenthesisToken.Reconstruct());
        writer.Write(DefaultKeywordToken?.Reconstruct() ?? "");
        writer.Write(DefaultOptionToken?.Reconstruct() ?? "");
        writer.Write(SemicolonToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"declare spectrum {Name} ({string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))}) {(DefaultOption is not null ? "default " : "")}{DefaultOption}";

    bool IOutcomeDeclarationNode.IsPublic => false;
}
