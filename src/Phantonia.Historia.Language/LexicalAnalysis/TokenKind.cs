﻿namespace Phantonia.Historia.Language.LexicalAnalysis;

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
    OutcomeKeyword,
    DefaultKeyword,
    SpectrumKeyword,
    StrengthenKeyword,
    WeakenKeyword,
    ByKeyword,
    CallKeyword,

    // literals
    Identifier,
    IntegerLiteral,
    StringLiteral,
    BrokenStringLiteral,
}
