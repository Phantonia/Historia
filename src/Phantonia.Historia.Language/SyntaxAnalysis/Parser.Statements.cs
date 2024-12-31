using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed partial class Parser
{
    private StatementBodyNode? ParseStatementBody(ref int index)
    {
        ImmutableArray<StatementNode>.Builder statementBuilder = ImmutableArray.CreateBuilder<StatementNode>();

        int nodeIndex = tokens[index].Index;

        Token openBrace = Expect(TokenKind.OpenBrace, ref index);

        while (tokens[index].Kind is not TokenKind.ClosedBrace)
        {
            StatementNode? nextStatement = ParseStatement(ref index);
            if (nextStatement is null)
            {
                return null;
            }

            statementBuilder.Add(nextStatement);
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

    private StatementNode? ParseStatement(ref int index)
    {
        switch (tokens[index].Kind)
        {
            case TokenKind.OutputKeyword:
                return ParseOutputStatement(ref index, checkpointToken: null);
            case TokenKind.SwitchKeyword:
                return ParseSwitchStatement(ref index, isCheckpoint: false);
            case TokenKind.LoopKeyword:
                return ParseLoopSwitchStatement(ref index, isCheckpoint: false);
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
                    (string name, ImmutableArray<SpectrumOptionNode> options, string? defaultOption, int nodeIndex) = ParseSpectrumDeclaration(ref index);
                    return new SpectrumDeclarationStatementNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        Index = nodeIndex,
                    };
                }
            case TokenKind.StrengthenKeyword:
                return ParseSpectrumAdjustmentStatement(strengthens: true, ref index);
            case TokenKind.WeakenKeyword:
                return ParseSpectrumAdjustmentStatement(strengthens: false, ref index);
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
                return null;
            default:
                {
                    ErrorFound?.Invoke(Errors.UnexpectedToken(tokens[index]));
                    index++;
                    return ParseStatement(ref index);
                }
        }
    }

    private OutputStatementNode? ParseOutputStatement(ref int index, Token? checkpointToken)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.OutputKeyword);
        Token outputKeyword = tokens[index];

        int nodeIndex = tokens[index].Index;

        index++;

        ExpressionNode outputExpression = ParseExpression(ref index);

        Token semicolon = Expect(TokenKind.Semicolon, ref index);

        return new OutputStatementNode
        {
            CheckpointKeywordToken = checkpointToken,
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

        int nodeIndex = tokens[index].Index;

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

    private SwitchStatementNode? ParseSwitchStatement(ref int index, Token? checkpointKeyword)
    {
        int nodeIndex = tokens[index].Index;

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

        int nodeIndex = tokens[index].Index;

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
            Arguments = (ImmutableArray<ArgumentNode>)arguments,
            Options = (ImmutableArray<OptionNode>)optionNodes,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<OptionNode> ParseOptions(ref int index)
    {
        ImmutableArray<OptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<OptionNode>();

        while (tokens[index] is TokenKind.OptionKeyword)
        {
            int nodeIndex = tokens[index].Index;

            _ = Expect(TokenKind.OptionKeyword, ref index);

            ExpressionNode expression = ParseExpression(ref index);
            StatementBodyNode body = ParseStatementBody(ref index);

            OptionNode optionNode;

            optionNode = new OptionNode()
            {
                Expression = expression,
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        if (optionBuilder.Count == 0)
        {
            ErrorFound?.Invoke(Errors.MustHaveAtLeastOneOption(tokens[index].Index));
        }

        return optionBuilder.ToImmutable();
    }

    private LoopSwitchStatementNode ParseLoopSwitchStatement(ref int index, bool isCheckpoint)
    {
        int nodeIndex = tokens[index].Index;

        Debug.Assert(tokens[index] is TokenKind.LoopKeyword);

        index++;

        _ = Expect(TokenKind.SwitchKeyword, ref index);
        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ExpressionNode expression = ParseExpression(ref index);

        _ = Expect(TokenKind.ClosedParenthesis, ref index);

        _ = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<LoopSwitchOptionNode> optionNodes = ParseLoopSwitchOptions(ref index);

        _ = Expect(TokenKind.ClosedBrace, ref index);

        return new LoopSwitchStatementNode
        {
            OutputExpression = expression,
            Options = (ImmutableArray<LoopSwitchOptionNode>)optionNodes,
            IsCheckpoint = isCheckpoint,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<LoopSwitchOptionNode> ParseLoopSwitchOptions(ref int index)
    {
        ImmutableArray<LoopSwitchOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<LoopSwitchOptionNode>();

        while (tokens[index] is not TokenKind.ClosedBrace)
        {
            int nodeIndex = tokens[index].Index;
            LoopSwitchOptionKind kind;

            switch (tokens[index])
            {
                case TokenKind.OptionKeyword:
                    kind = LoopSwitchOptionKind.None;
                    index++;
                    break;
                case TokenKind.LoopKeyword:
                    kind = LoopSwitchOptionKind.Loop;
                    index++;
                    _ = Expect(TokenKind.OptionKeyword, ref index);
                    break;
                case TokenKind.FinalKeyword:
                    kind = LoopSwitchOptionKind.Final;
                    index++;
                    _ = Expect(TokenKind.OptionKeyword, ref index);
                    break;
                default:
                    ErrorFound?.Invoke(Errors.ExpectedToken(tokens[index], TokenKind.ClosedBrace));
                    return optionBuilder.ToImmutable();
            }

            _ = Expect(TokenKind.OpenParenthesis, ref index);

            ExpressionNode? expression = ParseExpression(ref index);

            if (expression is null)
            {
                return null;
            }

            _ = Expect(TokenKind.ClosedParenthesis, ref index);

            StatementBodyNode? body = ParseStatementBody(ref index);

            if (body is null)
            {
                return null;
            }

            LoopSwitchOptionNode optionNode = new()
            {
                Kind = kind,
                Expression = expression,
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        return optionBuilder.ToImmutable();
    }

    private StatementNode? ParseCheckpointStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind == TokenKind.CheckpointKeyword);

        int nodeIndex = tokens[index].Index;
        index++;

        switch (tokens[index])
        {
            case TokenKind.OutputKeyword:
                OutputStatementNode? outputStatement = ParseOutputStatement(ref index, isCheckpoint: true);

                if (outputStatement is not null)
                {
                    outputStatement = outputStatement with
                    {
                        Index = nodeIndex,
                    };
                }

                return outputStatement;
            case TokenKind.SwitchKeyword:
                SwitchStatementNode? switchStatement = ParseSwitchStatement(ref index, isCheckpoint: true);

                if (switchStatement is not null)
                {
                    switchStatement = switchStatement with
                    {
                        Index = nodeIndex,
                    };
                }

                return switchStatement;
            case TokenKind.LoopKeyword:
                LoopSwitchStatementNode? loopSwitchStatement = ParseLoopSwitchStatement(ref index, isCheckpoint: true);

                if (loopSwitchStatement is not null)
                {
                    loopSwitchStatement = loopSwitchStatement with
                    {
                        Index = nodeIndex,
                    };
                }

                return loopSwitchStatement;
            default:
                ErrorFound?.Invoke(Errors.ExpectedVisibleStatementAsCheckpoint(tokens[index]));

                // ignore that we found a 'checkpoint' keyword and just try again
                return ParseStatement(ref index);
        }
    }

    private BranchOnStatementNode? ParseBranchOnStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind == TokenKind.BranchOnKeyword);

        int nodeIndex = tokens[index].Index;
        index++;

        Token outcomeIdentifier = Expect(TokenKind.Identifier, ref index);

        _ = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<BranchOnOptionNode>? options = ParseBranchOnOptions(ref index);
        if (options is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedBrace, ref index);

        return new BranchOnStatementNode
        {
            OutcomeName = outcomeIdentifier.Text,
            Options = (ImmutableArray<BranchOnOptionNode>)options,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<BranchOnOptionNode>? ParseBranchOnOptions(ref int index)
    {
        ImmutableArray<BranchOnOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<BranchOnOptionNode>();

        while (tokens[index] is TokenKind.OptionKeyword)
        {
            int nodeIndex = tokens[index].Index;

            index++;

            string name = Expect(TokenKind.Identifier, ref index).Text;

            StatementBodyNode? body = ParseStatementBody(ref index);

            if (body is null)
            {
                return null;
            }

            BranchOnOptionNode optionNode = new NamedBranchOnOptionNode()
            {
                OptionName = name,
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        if (tokens[index] is TokenKind.OtherKeyword)
        {
            int nodeIndex = tokens[index].Index;

            index++;

            StatementBodyNode? body = ParseStatementBody(ref index);

            if (body is null)
            {
                return null;
            }

            BranchOnOptionNode optionNode = new OtherBranchOnOptionNode()
            {
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

    private CallStatementNode? ParseCallStatement(ref int index)
    {
        Debug.Assert(tokens[index] is TokenKind.CallKeyword);

        int nodeIndex = tokens[index].Index;
        index++;

        string sceneName = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.Semicolon, ref index);

        return new CallStatementNode
        {
            SceneName = sceneName,
            Index = nodeIndex,
        };
    }

    private StatementNode? ParseIdentifierLeadStatement(ref int index)
    {
        Debug.Assert(tokens[index] is TokenKind.Identifier);
        Token identifier = tokens[index];

        int nodeIndex = tokens[index].Index;
        index++;

        // we might have more statements that begin with an identifier later
        // rewrite this method then
        _ = Expect(TokenKind.Equals, ref index);

        ExpressionNode? assignedExpression = ParseExpression(ref index);
        if (assignedExpression is null)
        {
            return null;
        }

        _ = Expect(TokenKind.Semicolon, ref index);

        return new AssignmentStatementNode
        {
            VariableName = identifier.Text,
            AssignedExpression = assignedExpression,
            Index = nodeIndex,
        };
    }

    private SpectrumAdjustmentStatementNode? ParseSpectrumAdjustmentStatement(bool strengthens, ref int index)
    {
        if (strengthens)
        {
            Debug.Assert(tokens[index] is TokenKind.StrengthenKeyword);
        }
        else
        {
            Debug.Assert(tokens[index] is TokenKind.WeakenKeyword);
        }

        int nodeIndex = tokens[index].Index;
        index++;

        string spectrumName = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.ByKeyword, ref index);

        ExpressionNode? adjustmentAmount = ParseExpression(ref index);

        if (adjustmentAmount is null)
        {
            return null;
        }

        _ = Expect(TokenKind.Semicolon, ref index);

        return new SpectrumAdjustmentStatementNode
        {
            SpectrumName = spectrumName,
            Strengthens = strengthens,
            AdjustmentAmount = adjustmentAmount,
            Index = nodeIndex,
        };
    }

    private IfStatementNode? ParseIfStatement(ref int index)
    {
        int nodeIndex = tokens[index].Index;
        index++;

        ExpressionNode? condition = ParseExpression(ref index);

        if (condition is null)
        {
            return null;
        }

        StatementBodyNode? thenBlock = ParseStatementBody(ref index);

        if (thenBlock is null)
        {
            return null;
        }

        if (tokens[index].Kind is not TokenKind.ElseKeyword)
        {
            return new IfStatementNode
            {
                Condition = condition,
                ThenBlock = thenBlock,
                ElseBlock = null,
                Index = nodeIndex,
            };
        }

        index++;

        StatementBodyNode? elseBlock;

        if (tokens[index].Kind is TokenKind.IfKeyword)
        {
            IfStatementNode? ifStatement = ParseIfStatement(ref index);

            if (ifStatement is null)
            {
                return null;
            }

            elseBlock = new StatementBodyNode
            {
                Index = ifStatement.Index,
                Statements = [ifStatement],
            };
        }
        else
        {
            elseBlock = ParseStatementBody(ref index);

            if (elseBlock is null)
            {
                return null;
            }
        }

        return new IfStatementNode
        {
            Condition = condition,
            ThenBlock = thenBlock,
            ElseBlock = elseBlock,
            Index = nodeIndex,
        };
    }
}
