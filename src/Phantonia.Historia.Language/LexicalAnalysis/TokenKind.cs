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
    Comma, // ,
    Equals, // =

    // keywords
    SceneKeyword,
    SettingKeyword,
    RecordKeyword,
    OutputKeyword,
    SwitchKeyword,
    OptionKeyword,
    BranchOnKeyword,
    OtherKeyword,

    // literals
    Identifier,
    IntegerLiteral,
    StringLiteral,
    BrokenStringLiteral,
}
