namespace Phantonia.Historia.Language.LexicalAnalysis;

public enum TokenKind
{
    // general
    Unknown,
    EndOfFile,
    Empty,

    // punctuation
    OpenBrace, // {
    ClosedBrace, // }
    Semicolon, // ;
    OpenParenthesis, // (
    ClosedParenthesis, // )

    // keywords
    SceneKeyword,
    OutputKeyword,
    SwitchKeyword,
    OptionKeyword,

    // literals
    Identifier,
    IntegerLiteral,
}
