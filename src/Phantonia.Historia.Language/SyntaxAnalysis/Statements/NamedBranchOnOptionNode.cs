using Phantonia.Historia.Language.LexicalAnalysis;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public sealed record NamedBranchOnOptionNode() : BranchOnOptionNode
{
    public required Token OptionNameToken { get; init; }

    public string OptionName => OptionNameToken.Text;

    internal override string ReconstructCore() => OptionKeywordToken.Reconstruct() + OptionNameToken.Reconstruct() + Body.Reconstruct();

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(OptionKeywordToken.Reconstruct());
        writer.Write(OptionNameToken.Reconstruct());
        Body.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"option {OptionName} w/ {Body.Statements.Length} statements";
}
