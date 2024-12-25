namespace Phantonia.Historia.Language.SyntaxAnalysis;

public interface ISyntaxNode : IReconstructable
{
    int Index { get; }
}
