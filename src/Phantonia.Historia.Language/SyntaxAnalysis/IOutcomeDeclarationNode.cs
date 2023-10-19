using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public interface IOutcomeDeclarationNode : ISyntaxNode
{
    bool Public { get; }

    string Name { get; }

    ImmutableArray<string> Options { get; }

    string? DefaultOption { get; }
}
