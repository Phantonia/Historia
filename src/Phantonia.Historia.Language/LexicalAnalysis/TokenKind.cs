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
    Colon, // :

    // keywords
    SceneKeyword,
    SettingKeyword,
    OutputKeyword,
    SwitchKeyword,
    OptionKeyword,

    // literals
    Identifier,
    IntegerLiteral,
}
