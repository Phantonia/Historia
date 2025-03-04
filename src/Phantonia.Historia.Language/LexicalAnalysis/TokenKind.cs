namespace Phantonia.Historia.Language.LexicalAnalysis;

public enum TokenKind
{
    // general
    Unknown,
    EndOfFile,
    Missing,

    // punctuation
    OpenBrace, // {
    ClosedBrace, // }
    Semicolon, // ;
    OpenParenthesis, // (
    ClosedParenthesis, // )
    OpenSquareBracket, // [
    ClosedSquareBracket, // ]
    Colon, // :
    Comma, // ,
    Equals, // =
    LessThan, // <
    LessThanOrEquals, // <=
    Slash, // /
    Dot, // .

    // keywords
    SceneKeyword,
    ChapterKeyword,
    SettingKeyword,
    LineKeyword,
    RecordKeyword,
    UnionKeyword,
    EnumKeyword,
    OutputKeyword,
    SwitchKeyword,
    OptionKeyword,
    FinalKeyword,
    LoopKeyword,
    ContinueKeyword,
    BranchOnKeyword,
    OtherKeyword,
    PublicKeyword,
    OutcomeKeyword,
    DefaultKeyword,
    SpectrumKeyword,
    StrengthenKeyword,
    WeakenKeyword,
    ByKeyword,
    CallKeyword,
    InterfaceKeyword,
    ReferenceKeyword,
    ActionKeyword,
    ChoiceKeyword,
    RunKeyword,
    ChooseKeyword,
    IsKeyword,
    AndKeyword,
    OrKeyword,
    NotKeyword,
    IfKeyword,
    ElseKeyword,

    // literals
    Identifier,
    IntegerLiteral,
    StringLiteral,
    BrokenStringLiteral,
}
