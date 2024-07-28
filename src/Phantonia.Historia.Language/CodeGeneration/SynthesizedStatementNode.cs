using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Generic;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed record SynthesizedStartStatementNode : StatementNode, IOutputStatementNode
{
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield break;
        }
    }

    // big hack: we pretend our empty expression is an int
    // this is only there so that everything doesn't blow up
    // due to source type equaling target type, we will simply generate default(T) where T is the output type
    public ExpressionNode OutputExpression { get; } = new TypedExpressionNode()
    {
        Expression = new SynthesizedEmptyExpressionNode { Index = 0 },
        Index = 0,
        SourceType = new BuiltinTypeSymbol { Index = 0, Name = "Int", Type = BuiltinType.Int },
        TargetType = new BuiltinTypeSymbol { Index = 0, Name = "Int", Type = BuiltinType.Int },
    };

    protected internal override string GetDebuggerDisplay() => $"Synthesized start statement";
}
