using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed record SynthesizedEmptyExpressionNode : ExpressionNode
{
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield break;
        }
    }

    protected internal override string GetDebuggerDisplay() => $"Synthesized empty expression";
}
