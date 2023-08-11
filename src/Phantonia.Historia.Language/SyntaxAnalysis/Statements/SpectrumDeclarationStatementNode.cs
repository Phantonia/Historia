using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public record SpectrumDeclarationStatementNode : StatementNode, ISpectrumDeclarationNode
{
    public SpectrumDeclarationStatementNode() { }

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

    public required string Name { get; init; }

    public required string? DefaultOption { get; init; }

    public override IEnumerable<SyntaxNode> Children => Options;

    ImmutableArray<string> IOutcomeDeclarationNode.Options => stringOptions;

    protected internal override string GetDebuggerDisplay() => $"declare spectrum {Name} ({string.Join(", ", Options.Select(o => o.GetDebuggerDisplay()))}) {(DefaultOption is not null ? "default " : "")}{DefaultOption}";
}
