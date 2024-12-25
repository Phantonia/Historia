using Phantonia.Historia.Language.LexicalAnalysis;

namespace Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;

public enum InterfaceMethodKind
{
    Action = TokenKind.ActionKeyword,
    Choice = TokenKind.ChoiceKeyword,
}
