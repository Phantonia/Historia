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
    Dot, // .

    // keywords
    SceneKeyword,
    SettingKeyword,
    RecordKeyword,
    UnionKeyword,
    EnumKeyword,
    OutputKeyword,
    SwitchKeyword,
    OptionKeyword,
    FinalKeyword,
    LoopKeyword,
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
    CheckpointKeyword,
    InterfaceKeyword,
    ReferenceKeyword,
    ActionKeyword,
    ChoiceKeyword,
    RunKeyword,
    ChooseKeyword,

    // literals
    Identifier,
    IntegerLiteral,
    StringLiteral,
    BrokenStringLiteral,
}
