using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public readonly record struct OutcomeDeclarationInfo(Token OutcomeKeyword, Token Name, Token OpenParenthesis, ImmutableArray<Token> Options, ImmutableArray<Token> Commas, Token ClosedParenthesis, Token? DefaultKeyword, Token? DefaultOption, Token Semicolon, long Index);
