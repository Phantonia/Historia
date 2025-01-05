namespace Phantonia.Historia.Language.SyntaxAnalysis;

public interface ISyntaxNode : IReconstructable
{
    long Index { get; }
}
