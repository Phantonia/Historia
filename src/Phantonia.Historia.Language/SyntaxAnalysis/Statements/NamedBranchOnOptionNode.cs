using Phantonia.Historia.Language.LexicalAnalysis;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record NamedBranchOnOptionNode() : BranchOnOptionNode
{
    public required Token OptionNameToken { get; init; }

    public string OptionName => OptionNameToken.Text;

    protected override void ReconstructCore(TextWriter writer)
    {
        OptionKeywordToken.Reconstruct(writer);
        OptionNameToken.Reconstruct(writer);
        Body.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"option {OptionName} w/ {Body.Statements.Length} statements";
}
