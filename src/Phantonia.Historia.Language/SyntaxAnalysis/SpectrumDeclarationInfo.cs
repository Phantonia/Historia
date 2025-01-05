using Phantonia.Historia.Language.LexicalAnalysis;
using System.Collections.Immutable;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public readonly record struct SpectrumDeclarationInfo(Token SpectrumKeyword, Token Name, Token OpenParenthesis, ImmutableArray<SpectrumOptionNode> Options, Token ClosedParenthesis, Token? DefaultKeyword, Token? DefaultOption, Token Semicolon, long Index);
