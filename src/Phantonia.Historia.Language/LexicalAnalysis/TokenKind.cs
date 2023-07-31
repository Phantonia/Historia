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
    LessThan, // <
    LessThanOrEquals, // <=
    Slash, // /

    // keywords
    SceneKeyword,
    SettingKeyword,
    RecordKeyword,
    UnionKeyword,
    OutputKeyword,
    SwitchKeyword,
    OptionKeyword,
    BranchOnKeyword,
    OtherKeyword,
    OutcomeKeyword,
    DefaultKeyword,
    SpectrumKeyword,
    StrengthenKeyword,
    WeakenKeyword,
    ByKeyword,

    // literals
    Identifier,
    IntegerLiteral,
    StringLiteral,
    BrokenStringLiteral,
}
