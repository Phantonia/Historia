using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Generic;
using System.IO;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Expressions;

public record EnumOptionExpressionNode() : ExpressionNode
{
    public required Token EnumNameToken { get; init; }

    public required Token DotToken { get; init; }

    public required Token OptionNameToken { get; init; }

    public string EnumName => EnumNameToken.Text;

    public string OptionName => OptionNameToken.Text;

    public override IEnumerable<SyntaxNode> Children => [];

    internal override string ReconstructCore() => EnumNameToken.Reconstruct() + DotToken.Reconstruct() + OptionNameToken.Reconstruct();

    internal override void ReconstructCore(TextWriter writer)
    {
        writer.Write(EnumNameToken.Reconstruct());
        writer.Write(DotToken.Reconstruct());
        writer.Write(OptionNameToken.Reconstruct());
    }

    protected internal override string GetDebuggerDisplay() => $"enum option {EnumName}.{OptionName}";
}
