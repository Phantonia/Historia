using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public interface IArgumentContainerNode : ISyntaxNode
{
    ImmutableArray<ArgumentNode> Arguments { get; init; }
}
