using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public interface ISpectrumDeclarationNode : IOutcomeDeclarationNode
{
    new ImmutableArray<SpectrumOptionNode> Options { get; }
}
