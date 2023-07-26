using System.Collections.Immutable;

namespace Phantonia.Historia.Language.GrammaticalAnalysis;

public interface IOutcomeDeclarationNode : ISyntaxNode
{
    string Name { get; }

    ImmutableArray<string> Options { get; }

    string? DefaultOption { get; }
}
