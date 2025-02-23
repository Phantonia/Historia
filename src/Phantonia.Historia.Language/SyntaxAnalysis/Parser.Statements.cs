using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed partial class Parser
{
    private StatementBodyNode ParseStatementBody(ref int index, ImmutableList<Token> precedingTokens)
    {
        ImmutableArray<StatementNode>.Builder statementBuilder = ImmutableArray.CreateBuilder<StatementNode>();

        long nodeIndex = tokens[index].Index;

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        while (tokens[index].Kind is not (TokenKind.ClosedBrace or TokenKind.EndOfFile))
        {
            StatementNode nextStatement = ParseStatement(ref index, []);

            statementBuilder.Add(nextStatement);
        }

        Token closedBrace = Expect(TokenKind.ClosedBrace, ref index);

        if (statementBuilder.Count == 0)
        {
            NoOpStatementNode noopStatement = new()
            {
                Index = nodeIndex + 1,
                PrecedingTokens = [],
            };
            statementBuilder.Add(noopStatement);
        }

        return new StatementBodyNode
        {
            OpenBraceToken = openBrace,
            Statements = statementBuilder.ToImmutable(),
            ClosedBraceToken = closedBrace,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private StatementNode ParseStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        switch (tokens[index].Kind)
        {
            case TokenKind.OutputKeyword:
                return ParseOutputStatement(ref index, precedingTokens);
            case TokenKind.SwitchKeyword:
                return ParseSwitchStatement(ref index, precedingTokens);
            case TokenKind.LoopKeyword:
                return ParseLoopSwitchStatement(ref index, precedingTokens);
            case TokenKind.BranchOnKeyword:
                return ParseBranchOnStatement(ref index, precedingTokens);
            case TokenKind.CallKeyword:
                return ParseCallStatement(ref index, precedingTokens);
            case TokenKind.OutcomeKeyword:
                {
                    OutcomeDeclarationInfo info = ParseOutcomeDeclaration(ref index);
                    return new OutcomeDeclarationStatementNode
                    {
                        OutcomeKeywordToken = info.OutcomeKeyword,
                        NameToken = info.Name,
                        OpenParenthesisToken = info.OpenParenthesis,
                        OptionNameTokens = info.Options,
                        CommaTokens = info.Commas,
                        ClosedParenthesisToken = info.ClosedParenthesis,
                        DefaultKeywordToken = info.DefaultKeyword,
                        DefaultOptionToken = info.DefaultOption,
                        SemicolonToken = info.Semicolon,
                        Index = info.Index,
                        PrecedingTokens = precedingTokens,
                    };
                }
            case TokenKind.SpectrumKeyword:
                {
                    SpectrumDeclarationInfo spectrumDeclaration = ParseSpectrumDeclaration(ref index);

                    return new SpectrumDeclarationStatementNode
                    {
                        SpectrumKeywordToken = spectrumDeclaration.SpectrumKeyword,
                        NameToken = spectrumDeclaration.Name,
                        OpenParenthesisToken = spectrumDeclaration.OpenParenthesis,
                        Options = spectrumDeclaration.Options,
                        ClosedParenthesisToken = spectrumDeclaration.ClosedParenthesis,
                        DefaultKeywordToken = spectrumDeclaration.DefaultKeyword,
                        DefaultOptionToken = spectrumDeclaration.DefaultOption,
                        SemicolonToken = spectrumDeclaration.Semicolon,
                        Index = spectrumDeclaration.Index,
                        PrecedingTokens = precedingTokens,
                    };
                }
            case TokenKind.StrengthenKeyword or TokenKind.WeakenKeyword:
                return ParseSpectrumAdjustmentStatement(ref index, precedingTokens);
            case TokenKind.Identifier:
                return ParseIdentifierLeadStatement(ref index, precedingTokens);
            case TokenKind.RunKeyword:
                return ParseRunStatement(ref index, precedingTokens);
            case TokenKind.ChooseKeyword:
                return ParseChooseStatement(ref index, precedingTokens);
            case TokenKind.IfKeyword:
                return ParseIfStatement(ref index, precedingTokens);
            case TokenKind.EndOfFile:
                ErrorFound?.Invoke(Errors.UnexpectedEndOfFile(tokens[index]));
                return new MissingStatementNode
                {
                    Index = tokens[index].Index,
                    PrecedingTokens = precedingTokens,
                };
            default:
                return ParseExpressionLeadStatement(ref index, precedingTokens);
        }
    }

    private OutputStatementNode ParseOutputStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.OutputKeyword);
        Token outputKeyword = tokens[index];

        long nodeIndex = tokens[index].Index;

        index++;

        ExpressionNode outputExpression = ParseExpression(ref index, []);

        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new OutputStatementNode
        {
            OutputKeywordToken = outputKeyword,
            OutputExpression = outputExpression,
            SemicolonToken = semicolon,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private RunStatementNode ParseRunStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.RunKeyword);
        Token runKeyword = tokens[index];

        long nodeIndex = tokens[index].Index;

        index++;

        Token referenceName = Expect(TokenKind.Identifier, ref index);

        Token dot = Expect(TokenKind.Dot, ref index);

        Token methodName = Expect(TokenKind.Identifier, ref index);

        (Token openParenthesis, ImmutableArray<ArgumentNode> arguments, Token closedParenthesis) = ParseArgumentList(ref index, TokenKind.OpenParenthesis, TokenKind.ClosedParenthesis, []);

        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new RunStatementNode
        {
            RunOrChooseKeywordToken = runKeyword,
            ReferenceNameToken = referenceName,
            DotToken = dot,
            MethodNameToken = methodName,
            OpenParenthesisToken = openParenthesis,
            Arguments = arguments,
            ClosedParenthesisToken = closedParenthesis,
            SemicolonToken = semicolon,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private SwitchStatementNode ParseSwitchStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        long nodeIndex = tokens[index].Index;

        Debug.Assert(tokens[index].Kind is TokenKind.SwitchKeyword);
        Token switchKeyword = tokens[index];

        index++;

        ExpressionNode expression = ParseExpression(ref index, []);

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<StatementNode>.Builder statementBuilder = ImmutableArray.CreateBuilder<StatementNode>();

        while (tokens[index].Kind is not (TokenKind.ClosedBrace or TokenKind.EndOfFile or TokenKind.OptionKeyword))
        {
            StatementNode nextStatement = ParseStatement(ref index, []);

            statementBuilder.Add(nextStatement);
        }

        ImmutableArray<OptionNode> optionNodes = ParseOptions(ref index, []);

        Token closedBrace = Expect(TokenKind.ClosedBrace, ref index);

        StatementBodyNode body = new()
        {
            OpenBraceToken = null,
            Statements = statementBuilder.ToImmutable(),
            ClosedBraceToken = null,
            Index = statementBuilder.Count > 0 ? statementBuilder[0].Index : optionNodes.Length > 0 ? optionNodes[0].Index : closedBrace.Index,
            PrecedingTokens = [],
        };

        return new SwitchStatementNode
        {
            SwitchKeywordToken = switchKeyword,
            OutputExpression = expression,
            OpenBraceToken = openBrace,
            Body = body,
            Options = optionNodes,
            ClosedBraceToken = closedBrace,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private ChooseStatementNode ParseChooseStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.ChooseKeyword);
        Token chooseKeyword = tokens[index];

        long nodeIndex = tokens[index].Index;

        index++;

        Token referenceName = Expect(TokenKind.Identifier, ref index);
        Token dot = Expect(TokenKind.Dot, ref index);
        Token methodName = Expect(TokenKind.Identifier, ref index);

        (Token openParenthesis, ImmutableArray<ArgumentNode> arguments, Token closedParenthesis) = ParseArgumentList(ref index, TokenKind.OpenParenthesis, TokenKind.ClosedParenthesis, []);

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<OptionNode> optionNodes = ParseOptions(ref index, []);

        Token closedBrace = Expect(TokenKind.ClosedBrace, ref index);

        return new ChooseStatementNode
        {
            RunOrChooseKeywordToken = chooseKeyword,
            ReferenceNameToken = referenceName,
            DotToken = dot,
            MethodNameToken = methodName,
            OpenParenthesisToken = openParenthesis,
            Arguments = arguments,
            ClosedParenthesisToken = closedParenthesis,
            OpenBraceToken = openBrace,
            Options = optionNodes,
            ClosedBraceToken = closedBrace,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private ImmutableArray<OptionNode> ParseOptions(ref int index, ImmutableList<Token> precedingTokens)
    {
        ImmutableArray<OptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<OptionNode>();

        while (tokens[index].Kind is TokenKind.OptionKeyword)
        {
            long nodeIndex = tokens[index].Index;

            Token optionKeyword = Expect(TokenKind.OptionKeyword, ref index);

            ExpressionNode expression = ParseExpression(ref index, []);
            StatementBodyNode body = ParseStatementBody(ref index, []);

            OptionNode optionNode;

            optionNode = new OptionNode()
            {
                OptionKeywordToken = optionKeyword,
                Expression = expression,
                Body = body,
                Index = nodeIndex,
                PrecedingTokens = precedingTokens,
            };

            optionBuilder.Add(optionNode);
        }

        if (optionBuilder.Count is 0)
        {
            ErrorFound?.Invoke(Errors.MustHaveAtLeastOneOption(tokens[index].Index));
        }

        return optionBuilder.ToImmutable();
    }

    private LoopSwitchStatementNode ParseLoopSwitchStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.LoopKeyword);
        Token loopKeyword = tokens[index];
        long nodeIndex = loopKeyword.Index;
        index++;

        Token switchKeyword = Expect(TokenKind.SwitchKeyword, ref index);
        ExpressionNode expression = ParseExpression(ref index, []);

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<LoopSwitchOptionNode> optionNodes = ParseLoopSwitchOptions(ref index, []);

        Token closedBrace = Expect(TokenKind.ClosedBrace, ref index);

        return new LoopSwitchStatementNode
        {
            LoopKeywordToken = loopKeyword,
            SwitchKeywordToken = switchKeyword,
            OutputExpression = expression,
            OpenBraceToken = openBrace,
            Options = optionNodes,
            ClosedBraceToken = closedBrace,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private ImmutableArray<LoopSwitchOptionNode> ParseLoopSwitchOptions(ref int index, ImmutableList<Token> precedingTokens)
    {
        ImmutableArray<LoopSwitchOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<LoopSwitchOptionNode>();

        while (tokens[index].Kind is not TokenKind.ClosedBrace)
        {
            long nodeIndex = tokens[index].Index;

            Token? kind;
            Token optionKeyword;

            switch (tokens[index].Kind)
            {
                case TokenKind.OptionKeyword:
                    kind = null;
                    optionKeyword = tokens[index];
                    index++;
                    break;
                case TokenKind.LoopKeyword or TokenKind.FinalKeyword:
                    kind = tokens[index];
                    index++;
                    optionKeyword = Expect(TokenKind.OptionKeyword, ref index);
                    break;
                default:
                    ErrorFound?.Invoke(Errors.ExpectedToken(tokens[index], TokenKind.ClosedBrace));
                    return optionBuilder.ToImmutable();
            }

            ExpressionNode expression = ParseExpression(ref index, []);
            StatementBodyNode body = ParseStatementBody(ref index, []);

            LoopSwitchOptionNode optionNode = new()
            {
                KindToken = kind,
                OptionKeywordToken = optionKeyword,
                Expression = expression,
                Body = body,
                Index = nodeIndex,
                PrecedingTokens = precedingTokens,
            };

            optionBuilder.Add(optionNode);
        }

        return optionBuilder.ToImmutable();
    }

    private BranchOnStatementNode ParseBranchOnStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind == TokenKind.BranchOnKeyword);
        Token branchonKeyword = tokens[index];
        long nodeIndex = branchonKeyword.Index;
        index++;

        Token outcomeName = Expect(TokenKind.Identifier, ref index);

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<BranchOnOptionNode> options = ParseBranchOnOptions(ref index, []);

        Token closedBrace = Expect(TokenKind.ClosedBrace, ref index);

        return new BranchOnStatementNode
        {
            BranchOnKeywordToken = branchonKeyword,
            OutcomeNameToken = outcomeName,
            OpenBraceToken = openBrace,
            Options = options,
            ClosedBraceToken = closedBrace,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private ImmutableArray<BranchOnOptionNode> ParseBranchOnOptions(ref int index, ImmutableList<Token> precedingTokens)
    {
        ImmutableArray<BranchOnOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<BranchOnOptionNode>();

        while (tokens[index].Kind is TokenKind.OptionKeyword)
        {
            Token optionKeyword = tokens[index];
            long nodeIndex = optionKeyword.Index;
            index++;

            Token optionName = Expect(TokenKind.Identifier, ref index);

            StatementBodyNode? body = ParseStatementBody(ref index, []);

            BranchOnOptionNode optionNode = new NamedBranchOnOptionNode()
            {
                OptionKeywordToken = optionKeyword,
                OptionNameToken = optionName,
                Body = body,
                Index = nodeIndex,
                PrecedingTokens = precedingTokens,
            };

            optionBuilder.Add(optionNode);
        }

        if (tokens[index].Kind is TokenKind.OtherKeyword)
        {
            Token otherKeyword = tokens[index];
            long nodeIndex = otherKeyword.Index;

            index++;

            StatementBodyNode body = ParseStatementBody(ref index, []);

            BranchOnOptionNode optionNode = new OtherBranchOnOptionNode()
            {
                OptionKeywordToken = Token.Missing(nodeIndex),
                OtherKeywordToken = otherKeyword,
                Body = body,
                Index = nodeIndex,
                PrecedingTokens = precedingTokens,
            };

            optionBuilder.Add(optionNode);
        }

        if (tokens[index] is { Kind: TokenKind.OptionKeyword or TokenKind.OtherKeyword })
        {
            ErrorFound?.Invoke(Errors.BranchOnOnlyOneOtherLast(tokens[index].Index));
        }

        return optionBuilder.ToImmutable();
    }

    private CallStatementNode ParseCallStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.CallKeyword);
        Token callKeyword = tokens[index];
        long nodeIndex = tokens[index].Index;
        index++;

        Token subroutineName = Expect(TokenKind.Identifier, ref index);
        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new CallStatementNode
        {
            CallKeywordToken = callKeyword,
            SubroutineNameToken = subroutineName,
            SemicolonToken = semicolon,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private StatementNode ParseIdentifierLeadStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.Identifier);
        Token identifier = tokens[index];

        long nodeIndex = tokens[index].Index;
        index++;

        if (tokens[index].Kind is TokenKind.OpenSquareBracket or TokenKind.Colon)
        {
            IdentifierExpressionNode identifierExpression = new()
            {
                IdentifierToken = identifier,
                Index = nodeIndex,
                PrecedingTokens = precedingTokens,
            };

            return ParseLineStatement(ref index, identifierExpression);
        }
        else
        {
            Token equals = tokens[index];
            index++;

            ExpressionNode assignedExpression = ParseExpression(ref index, []);

            Token semicolon = Expect(TokenKind.Semicolon, ref index);

            return new AssignmentStatementNode
            {
                VariableNameToken = identifier,
                EqualsSignToken = equals,
                AssignedExpression = assignedExpression,
                SemicolonToken = semicolon,
                Index = nodeIndex,
                PrecedingTokens = precedingTokens,
            };
        }
    }

    private SpectrumAdjustmentStatementNode ParseSpectrumAdjustmentStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.StrengthenKeyword or TokenKind.WeakenKeyword);
        Token adjustmentKeyword = tokens[index];
        long nodeIndex = adjustmentKeyword.Index;
        index++;

        Token spectrumName = Expect(TokenKind.Identifier, ref index);
        Token byKeyword = Expect(TokenKind.ByKeyword, ref index);

        ExpressionNode adjustmentAmount = ParseExpression(ref index, []);

        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new SpectrumAdjustmentStatementNode
        {
            StrengthenOrWeakenKeywordToken = adjustmentKeyword,
            SpectrumNameToken = spectrumName,
            ByKeywordToken = byKeyword,
            AdjustmentAmount = adjustmentAmount,
            SemicolonToken = semicolon,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private IfStatementNode ParseIfStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.IfKeyword);

        Token ifKeyword = tokens[index];
        long nodeIndex = ifKeyword.Index;
        index++;

        ExpressionNode condition = ParseExpression(ref index, []);

        StatementBodyNode thenBlock = ParseStatementBody(ref index, []);

        if (tokens[index].Kind is not TokenKind.ElseKeyword)
        {
            NoOpStatementNode noopStatement = new()
            {
                Index = nodeIndex + 2,
                PrecedingTokens = [],
            };

            StatementBodyNode emptyElseBlock = new()
            {
                OpenBraceToken = Token.Missing(nodeIndex + 1),
                Statements = [noopStatement],
                ClosedBraceToken = Token.Missing(tokens[index].Index),
                Index = nodeIndex + 1,
                PrecedingTokens = precedingTokens,
            };

            return new IfStatementNode
            {
                IfKeywordToken = ifKeyword,
                Condition = condition,
                ThenBlock = thenBlock,
                ElseKeywordToken = Token.Missing(nodeIndex + 1),
                ElseBlock = emptyElseBlock,
                Index = nodeIndex,
                PrecedingTokens = precedingTokens,
            };
        }

        Token elseKeyword = tokens[index];

        index++;

        StatementBodyNode elseBlock;

        if (tokens[index].Kind is TokenKind.IfKeyword)
        {
            IfStatementNode ifStatement = ParseIfStatement(ref index, []);

            elseBlock = new StatementBodyNode
            {
                OpenBraceToken = Token.Missing(ifStatement.Index),
                Statements = [ifStatement],
                ClosedBraceToken = Token.Missing(tokens[index].Index),
                Index = ifStatement.Index,
                PrecedingTokens = precedingTokens,
            };
        }
        else
        {
            elseBlock = ParseStatementBody(ref index, []);
        }

        return new IfStatementNode
        {
            IfKeywordToken = ifKeyword,
            Condition = condition,
            ThenBlock = thenBlock,
            ElseKeywordToken = elseKeyword,
            ElseBlock = elseBlock,
            Index = nodeIndex,
            PrecedingTokens = precedingTokens,
        };
    }

    private StatementNode ParseExpressionLeadStatement(ref int index, ImmutableList<Token> precedingTokens)
    {
        ExpressionNode expression = ParseExpression(ref index, precedingTokens);

        // currently, the only way to continue this is by parsing a line statement
        return ParseLineStatement(ref index, expression);
    }

    private LineStatementNode ParseLineStatement(ref int index, ExpressionNode characterExpression)
    {
        Token? openSquareBracket = null;
        ImmutableArray<ArgumentNode>? additionalArguments = null;
        Token? closedSquareBracket = null;

        if (tokens[index].Kind is TokenKind.OpenSquareBracket)
        {
            (openSquareBracket, additionalArguments, closedSquareBracket) = ParseArgumentList(ref index, TokenKind.OpenSquareBracket, TokenKind.ClosedSquareBracket, []);
        }

        Token colon = Expect(TokenKind.Colon, ref index);
        ExpressionNode textExpression = ParseExpression(ref index, []);
        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new LineStatementNode
        {
            CharacterExpression = characterExpression,
            OpenSquareBracketToken = openSquareBracket,
            AdditionalArguments = additionalArguments,
            ClosedSquareBracketToken = closedSquareBracket,
            ColonToken = colon,
            TextExpression = textExpression,
            SemicolonToken = semicolon,
            PrecedingTokens = [],
            Index = characterExpression.Index,
        };
    }
}
