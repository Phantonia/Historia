namespace Phantonia.Historia.Language.Ast;

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

    // literals
    Identifier,
    IntegerLiteral,
}
