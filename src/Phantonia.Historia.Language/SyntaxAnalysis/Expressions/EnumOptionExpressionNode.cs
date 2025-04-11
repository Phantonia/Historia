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

    public override bool IsConstant => true;

    public override IEnumerable<SyntaxNode> Children => [];

    protected override void ReconstructCore(TextWriter writer)
    {
        EnumNameToken.Reconstruct(writer);
        DotToken.Reconstruct(writer);
        OptionNameToken.Reconstruct(writer);
    }

    protected internal override string GetDebuggerDisplay() => $"enum option {EnumName}.{OptionName}";
}
