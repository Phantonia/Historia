using Phantonia.Historia.Language.SyntaxAnalysis;

namespace Phantonia.Historia.Language;

public interface IRefactoring
{
    StoryNode Refactor(StoryNode original);
}
