using Phantonia.Historia.Language.LexicalAnalysis;

namespace Phantonia.Historia.Language.SyntaxAnalysis.Statements;

public enum LoopSwitchOptionKind
{
    None = 0,
    Loop = TokenKind.LoopKeyword,
    Final = TokenKind.FinalKeyword,
}
