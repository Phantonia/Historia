using Phantonia.Historia.Language.GrammaticalAnalysis;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language;

public interface ISpectrumDeclarationNode : IOutcomeDeclarationNode
{
    new ImmutableArray<SpectrumOptionNode> Options { get; }
}
