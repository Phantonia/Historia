using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed partial class Parser
{
    private StatementBodyNode ParseStatementBody(ref int index)
    {
        ImmutableArray<StatementNode>.Builder statementBuilder = ImmutableArray.CreateBuilder<StatementNode>();

        long nodeIndex = tokens[index].Index;

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        while (tokens[index].Kind is not TokenKind.ClosedBrace)
        {
            StatementNode nextStatement = ParseStatement(ref index);
            
            statementBuilder.Add(nextStatement);

            if (tokens[index].Kind is TokenKind.EndOfFile)
            {
                return new StatementBodyNode
                {
                    OpenBraceToken = openBrace,
                    Statements = statementBuilder.ToImmutable(),
                    ClosedBraceToken = Token.Missing(tokens[index].Index),
                    Index = nodeIndex,
                };
            }
        }

        Token closedBrace = tokens[index];
        index++;

        return new StatementBodyNode
        {
            OpenBraceToken = openBrace,
            Statements = statementBuilder.ToImmutable(),
            ClosedBraceToken = closedBrace,
            Index = nodeIndex,
        };
    }

    private StatementNode ParseStatement(ref int index)
    {
        switch (tokens[index].Kind)
        {
            case TokenKind.OutputKeyword:
                return ParseOutputStatement(ref index, checkpointKeyword: null);
            case TokenKind.SwitchKeyword:
                return ParseSwitchStatement(ref index, checkpointKeyword: null);
            case TokenKind.LoopKeyword:
                return ParseLoopSwitchStatement(ref index, checkpointKeyword: null);
            case TokenKind.CheckpointKeyword:
                return ParseCheckpointStatement(ref index);
            case TokenKind.BranchOnKeyword:
                return ParseBranchOnStatement(ref index);
            case TokenKind.CallKeyword:
                return ParseCallStatement(ref index);
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
                    };
                }
            case TokenKind.StrengthenKeyword or TokenKind.WeakenKeyword:
                return ParseSpectrumAdjustmentStatement(ref index);
            case TokenKind.Identifier:
                return ParseIdentifierLeadStatement(ref index);
            case TokenKind.RunKeyword:
                return ParseRunStatement(ref index);
            case TokenKind.ChooseKeyword:
                return ParseChooseStatement(ref index);
            case TokenKind.IfKeyword:
                return ParseIfStatement(ref index);
            case TokenKind.EndOfFile:
                ErrorFound?.Invoke(Errors.UnexpectedEndOfFile(tokens[index]));
                return new MissingStatementNode
                {
                    Index = tokens[index++].Index,
                };
            default:
                {
                    ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index]));
                    index++;
                    return ParseStatement(ref index);
                }
        }
    }

    private OutputStatementNode ParseOutputStatement(ref int index, Token? checkpointKeyword)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.OutputKeyword);
        Token outputKeyword = tokens[index];

        long nodeIndex = tokens[index].Index;

        index++;

        ExpressionNode outputExpression = ParseExpression(ref index);

        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new OutputStatementNode
        {
            CheckpointKeywordToken = checkpointKeyword,
            OutputKeywordToken = outputKeyword,
            OutputExpression = outputExpression,
            SemicolonToken = semicolon,
            Index = nodeIndex,
        };
    }

    private RunStatementNode ParseRunStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.RunKeyword);
        Token runKeyword = tokens[index];

        long nodeIndex = tokens[index].Index;

        index++;

        Token referenceName = Expect(TokenKind.Identifier, ref index);

        Token dot = Expect(TokenKind.Dot, ref index);

        Token methodName = Expect(TokenKind.Identifier, ref index);

        (Token openParenthesis, ImmutableArray<ArgumentNode> arguments, Token closedParenthesis) = ParseArgumentList(ref index);

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
        };
    }

    private SwitchStatementNode ParseSwitchStatement(ref int index, Token? checkpointKeyword)
    {
        long nodeIndex = tokens[index].Index;

        Debug.Assert(tokens[index].Kind is TokenKind.SwitchKeyword);
        Token switchKeyword = tokens[index];

        index++;

        ExpressionNode expression = ParseExpression(ref index);

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<OptionNode> optionNodes = ParseOptions(ref index);

        Token closedBrace = Expect(TokenKind.ClosedBrace, ref index);

        return new SwitchStatementNode
        {
            CheckpointKeywordToken = checkpointKeyword,
            SwitchKeywordToken = switchKeyword,
            OutputExpression = expression,
            OpenBraceToken = openBrace,
            Options = optionNodes,
            ClosedBraceToken = closedBrace,
            Index = nodeIndex,
        };
    }

    private ChooseStatementNode ParseChooseStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.ChooseKeyword);
        Token chooseKeyword = tokens[index];

        long nodeIndex = tokens[index].Index;

        index++;

        Token referenceName = Expect(TokenKind.Identifier, ref index);
        Token dot = Expect(TokenKind.Dot, ref index);
        Token methodName = Expect(TokenKind.Identifier, ref index);

        (Token openParenthesis, ImmutableArray<ArgumentNode> arguments, Token closedParenthesis) = ParseArgumentList(ref index);

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<OptionNode> optionNodes = ParseOptions(ref index);

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
        };
    }

    private ImmutableArray<OptionNode> ParseOptions(ref int index)
    {
        ImmutableArray<OptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<OptionNode>();

        while (tokens[index].Kind is TokenKind.OptionKeyword)
        {
            long nodeIndex = tokens[index].Index;

            Token optionKeyword = Expect(TokenKind.OptionKeyword, ref index);

            ExpressionNode expression = ParseExpression(ref index);
            StatementBodyNode body = ParseStatementBody(ref index);

            OptionNode optionNode;

            optionNode = new OptionNode()
            {
                OptionKeywordToken = optionKeyword,
                Expression = expression,
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        if (optionBuilder.Count is 0)
        {
            ErrorFound?.Invoke(Errors.MustHaveAtLeastOneOption(tokens[index].Index));
        }

        return optionBuilder.ToImmutable();
    }

    private LoopSwitchStatementNode ParseLoopSwitchStatement(ref int index, Token? checkpointKeyword)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.LoopKeyword);
        Token loopKeyword = tokens[index];
        long nodeIndex = loopKeyword.Index;
        index++;

        Token switchKeyword = Expect(TokenKind.SwitchKeyword, ref index);
        ExpressionNode expression = ParseExpression(ref index);

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<LoopSwitchOptionNode> optionNodes = ParseLoopSwitchOptions(ref index);

        Token closedBrace = Expect(TokenKind.ClosedBrace, ref index);

        return new LoopSwitchStatementNode
        {
            CheckpointKeywordToken = checkpointKeyword,
            LoopKeywordToken = loopKeyword,
            SwitchKeywordToken = switchKeyword,
            OutputExpression = expression,
            OpenBraceToken = openBrace,
            Options = optionNodes,
            ClosedBraceToken = closedBrace,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<LoopSwitchOptionNode> ParseLoopSwitchOptions(ref int index)
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

            ExpressionNode expression = ParseExpression(ref index);
            StatementBodyNode body = ParseStatementBody(ref index);

            LoopSwitchOptionNode optionNode = new()
            {
                KindToken = kind,
                OptionKeywordToken = optionKeyword,
                Expression = expression,
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        return optionBuilder.ToImmutable();
    }

    private StatementNode ParseCheckpointStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind == TokenKind.CheckpointKeyword);
        Token checkpointKeyword = tokens[index];
        long nodeIndex = checkpointKeyword.Index;
        index++;

        switch (tokens[index].Kind)
        {
            case TokenKind.OutputKeyword:
                OutputStatementNode outputStatement = ParseOutputStatement(ref index, checkpointKeyword);

                outputStatement = outputStatement with
                {
                    Index = nodeIndex,
                };

                return outputStatement;
            case TokenKind.SwitchKeyword:
                SwitchStatementNode switchStatement = ParseSwitchStatement(ref index, checkpointKeyword);

                switchStatement = switchStatement with
                {
                    Index = nodeIndex,
                };

                return switchStatement;
            case TokenKind.LoopKeyword:
                LoopSwitchStatementNode? loopSwitchStatement = ParseLoopSwitchStatement(ref index, checkpointKeyword);

                loopSwitchStatement = loopSwitchStatement with
                {
                    Index = nodeIndex,
                };

                return loopSwitchStatement;
            default:
                ErrorFound?.Invoke(Errors.ExpectedVisibleStatementAsCheckpoint(tokens[index]));

                // ignore that we found a 'checkpoint' keyword and just try again
                return ParseStatement(ref index);
        }
    }

    private BranchOnStatementNode ParseBranchOnStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind == TokenKind.BranchOnKeyword);
        Token branchonKeyword = tokens[index];
        long nodeIndex = branchonKeyword.Index;
        index++;

        Token outcomeName = Expect(TokenKind.Identifier, ref index);

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<BranchOnOptionNode> options = ParseBranchOnOptions(ref index);
        
        Token closedBrace = Expect(TokenKind.ClosedBrace, ref index);

        return new BranchOnStatementNode
        {
            BranchOnKeywordToken = branchonKeyword,
            OutcomeNameToken = outcomeName,
            OpenBraceToken = openBrace,
            Options = options,
            ClosedBraceToken = closedBrace,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<BranchOnOptionNode> ParseBranchOnOptions(ref int index)
    {
        ImmutableArray<BranchOnOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<BranchOnOptionNode>();

        while (tokens[index].Kind is TokenKind.OptionKeyword)
        {
            Token optionKeyword = tokens[index];
            long nodeIndex = optionKeyword.Index;
            index++;

            Token optionName = Expect(TokenKind.Identifier, ref index);

            StatementBodyNode? body = ParseStatementBody(ref index);

            BranchOnOptionNode optionNode = new NamedBranchOnOptionNode()
            {
                OptionKeywordToken = optionKeyword,
                OptionNameToken = optionName,
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        if (tokens[index].Kind is TokenKind.OtherKeyword)
        {
            Token otherKeyword = tokens[index];
            long nodeIndex = otherKeyword.Index;

            index++;

            StatementBodyNode body = ParseStatementBody(ref index);

            BranchOnOptionNode optionNode = new OtherBranchOnOptionNode()
            {
                OptionKeywordToken = Token.Missing(nodeIndex),
                OtherKeywordToken = otherKeyword,
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        if (tokens[index] is { Kind: TokenKind.OptionKeyword or TokenKind.OtherKeyword })
        {
            ErrorFound?.Invoke(Errors.BranchOnOnlyOneOtherLast(tokens[index].Index));
        }

        return optionBuilder.ToImmutable();
    }

    private CallStatementNode ParseCallStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.CallKeyword);
        Token callKeyword = tokens[index];
        long nodeIndex = tokens[index].Index;
        index++;

        Token sceneName = Expect(TokenKind.Identifier, ref index);
        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new CallStatementNode
        {
            CallKeywordToken = callKeyword,
            SceneNameToken = sceneName,
            SemicolonToken = semicolon,
            Index = nodeIndex,
        };
    }

    private StatementNode ParseIdentifierLeadStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.Identifier);
        Token identifier = tokens[index];

        long nodeIndex = tokens[index].Index;
        index++;

        // we might have more statements that begin with an identifier later
        // rewrite this method then
        Token equals = Expect(TokenKind.Equals, ref index);

        ExpressionNode assignedExpression = ParseExpression(ref index);
       
        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new AssignmentStatementNode
        {
            VariableNameToken = identifier,
            EqualsSignToken = equals,
            AssignedExpression = assignedExpression,
            SemicolonToken = semicolon,
            Index = nodeIndex,
        };
    }

    private SpectrumAdjustmentStatementNode ParseSpectrumAdjustmentStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.StrengthenKeyword or TokenKind.WeakenKeyword);
        Token adjustmentKeyword = tokens[index];
        long nodeIndex = adjustmentKeyword.Index;
        index++;

        Token spectrumName = Expect(TokenKind.Identifier, ref index);
        Token byKeyword = Expect(TokenKind.ByKeyword, ref index);

        ExpressionNode adjustmentAmount = ParseExpression(ref index);

        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new SpectrumAdjustmentStatementNode
        {
            StrengthenOrWeakenKeywordToken = adjustmentKeyword,
            SpectrumNameToken = spectrumName,
            ByKeywordToken = byKeyword,
            AdjustmentAmount = adjustmentAmount,
            SemicolonToken = semicolon,
            Index = nodeIndex,
        };
    }

    private IfStatementNode ParseIfStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.IfKeyword);

        Token ifKeyword = tokens[index];
        long nodeIndex = ifKeyword.Index;
        index++;

        ExpressionNode condition = ParseExpression(ref index);

        StatementBodyNode thenBlock = ParseStatementBody(ref index);

        if (tokens[index].Kind is not TokenKind.ElseKeyword)
        {
            return new IfStatementNode
            {
                IfKeywordToken = ifKeyword,
                Condition = condition,
                ThenBlock = thenBlock,
                ElseKeywordToken = null,
                ElseBlock = null,
                Index = nodeIndex,
            };
        }

        Token elseKeyword = tokens[index];

        index++;

        StatementBodyNode elseBlock;

        if (tokens[index].Kind is TokenKind.IfKeyword)
        {
            IfStatementNode ifStatement = ParseIfStatement(ref index);

            elseBlock = new StatementBodyNode
            {
                OpenBraceToken = Token.Missing(ifStatement.Index),
                Index = ifStatement.Index,
                ClosedBraceToken = Token.Missing(tokens[index].Index),
                Statements = [ifStatement],
            };
        }
        else
        {
            elseBlock = ParseStatementBody(ref index);
        }

        return new IfStatementNode
        {
            IfKeywordToken = ifKeyword,
            Condition = condition,
            ThenBlock = thenBlock,
            ElseKeywordToken = elseKeyword,
            ElseBlock = elseBlock,
            Index = nodeIndex,
        };
    }
}
