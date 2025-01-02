﻿using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed partial class Parser
{
    private StatementBodyNode? ParseStatementBody(ref int index)
    {
        ImmutableArray<StatementNode>.Builder statementBuilder = ImmutableArray.CreateBuilder<StatementNode>();

        long nodeIndex = tokens[index].Index;

        _ = Expect(TokenKind.OpenBrace, ref index);

        while (tokens[index].Kind is not TokenKind.ClosedBrace)
        {
            StatementNode? nextStatement = ParseStatement(ref index);
            if (nextStatement is null)
            {
                return null;
            }

            statementBuilder.Add(nextStatement);
        }

        // now we have the }
        index++;

        return new StatementBodyNode
        {
            Statements = statementBuilder.ToImmutable(),
            Index = nodeIndex,
        };
    }

    private StatementNode? ParseStatement(ref int index)
    {
        switch (tokens[index])
        {
            case { Kind: TokenKind.OutputKeyword }:
                return ParseOutputStatement(ref index, isCheckpoint: false);
            case { Kind: TokenKind.SwitchKeyword }:
                return ParseSwitchStatement(ref index, isCheckpoint: false);
            case { Kind: TokenKind.LoopKeyword }:
                return ParseLoopSwitchStatement(ref index, isCheckpoint: false);
            case { Kind: TokenKind.CheckpointKeyword }:
                return ParseCheckpointStatement(ref index);
            case { Kind: TokenKind.BranchOnKeyword }:
                return ParseBranchOnStatement(ref index);
            case { Kind: TokenKind.CallKeyword }:
                return ParseCallStatement(ref index);
            case { Kind: TokenKind.OutcomeKeyword }:
                {
                    (string name, ImmutableArray<string> options, string? defaultOption, long nodeIndex) = ParseOutcomeDeclaration(ref index);
                    return new OutcomeDeclarationStatementNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        Index = nodeIndex,
                    };
                }
            case { Kind: TokenKind.SpectrumKeyword }:
                {
                    (string name, ImmutableArray<SpectrumOptionNode> options, string? defaultOption, long nodeIndex) = ParseSpectrumDeclaration(ref index);
                    return new SpectrumDeclarationStatementNode
                    {
                        Name = name,
                        Options = options,
                        DefaultOption = defaultOption,
                        Index = nodeIndex,
                    };
                }
            case { Kind: TokenKind.StrengthenKeyword }:
                return ParseSpectrumAdjustmentStatement(strengthens: true, ref index);
            case { Kind: TokenKind.WeakenKeyword }:
                return ParseSpectrumAdjustmentStatement(strengthens: false, ref index);
            case { Kind: TokenKind.Identifier }:
                return ParseIdentifierLeadStatement(ref index);
            case { Kind: TokenKind.RunKeyword }:
                return ParseRunStatement(ref index);
            case { Kind: TokenKind.ChooseKeyword }:
                return ParseChooseStatement(ref index);
            case { Kind: TokenKind.IfKeyword }:
                return ParseIfStatement(ref index);
            case { Kind: TokenKind.EndOfFile }:
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

    private OutputStatementNode? ParseOutputStatement(ref int index, bool isCheckpoint)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.OutputKeyword);

        long nodeIndex = tokens[index].Index;

        index++;

        ExpressionNode? outputExpression = ParseExpression(ref index);
        if (outputExpression is null)
        {
            return null;
        }

        _ = Expect(TokenKind.Semicolon, ref index);

        return new OutputStatementNode
        {
            OutputExpression = outputExpression,
            IsCheckpoint = isCheckpoint,
            Index = nodeIndex,
        };
    }

    private RunStatementNode? ParseRunStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.RunKeyword);

        long nodeIndex = tokens[index].Index;

        index++;

        string referenceName = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.Dot, ref index);

        string methodName = Expect(TokenKind.Identifier, ref index).Text;

        ImmutableArray<ArgumentNode>? arguments = ParseArgumentList(ref index);

        if (arguments is null)
        {
            return null;
        }

        _ = Expect(TokenKind.Semicolon, ref index);

        return new RunStatementNode
        {
            ReferenceName = referenceName,
            MethodName = methodName,
            Arguments = (ImmutableArray<ArgumentNode>)arguments,
            Index = nodeIndex,
        };
    }

    private SwitchStatementNode? ParseSwitchStatement(ref int index, bool isCheckpoint)
    {
        long nodeIndex = tokens[index].Index;

        Debug.Assert(tokens[index] is { Kind: TokenKind.SwitchKeyword });

        index++;

        ExpressionNode? expression = ParseExpression(ref index);

        if (expression is null)
        {
            return null;
        }

        _ = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<OptionNode>? optionNodes = ParseOptions(ref index);

        if (optionNodes is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedBrace, ref index);

        return new SwitchStatementNode
        {
            OutputExpression = expression,
            Options = (ImmutableArray<OptionNode>)optionNodes,
            IsCheckpoint = isCheckpoint,
            Index = nodeIndex,
        };
    }

    private ChooseStatementNode? ParseChooseStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.ChooseKeyword);

        long nodeIndex = tokens[index].Index;

        index++;

        string referenceName = Expect(TokenKind.Identifier, ref index).Text;

        _ = Expect(TokenKind.Dot, ref index);

        string methodName = Expect(TokenKind.Identifier, ref index).Text;

        ImmutableArray<ArgumentNode>? arguments = ParseArgumentList(ref index);

        if (arguments is null)
        {
            return null;
        }

        _ = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<OptionNode>? optionNodes = ParseOptions(ref index);

        if (optionNodes is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedBrace, ref index);

        return new ChooseStatementNode
        {
            ReferenceName = referenceName,
            MethodName = methodName,
            Arguments = (ImmutableArray<ArgumentNode>)arguments,
            Options = (ImmutableArray<OptionNode>)optionNodes,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<OptionNode>? ParseOptions(ref int index)
    {
        ImmutableArray<OptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<OptionNode>();

        while (tokens[index] is { Kind: TokenKind.OptionKeyword })
        {
            long nodeIndex = tokens[index].Index;

            _ = Expect(TokenKind.OptionKeyword, ref index);

            ExpressionNode? expression = ParseExpression(ref index);

            if (expression is null)
            {
                return null;
            }

            StatementBodyNode? body = ParseStatementBody(ref index);

            if (body is null)
            {
                return null;
            }

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

    private LoopSwitchStatementNode? ParseLoopSwitchStatement(ref int index, bool isCheckpoint)
    {
        long nodeIndex = tokens[index].Index;

        Debug.Assert(tokens[index] is { Kind: TokenKind.LoopKeyword });

        index++;

        _ = Expect(TokenKind.SwitchKeyword, ref index);
        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ExpressionNode? expression = ParseExpression(ref index);

        if (expression is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedParenthesis, ref index);

        _ = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<LoopSwitchOptionNode>? optionNodes = ParseLoopSwitchOptions(ref index);

        if (optionNodes is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedBrace, ref index);

        return new LoopSwitchStatementNode
        {
            OutputExpression = expression,
            Options = (ImmutableArray<LoopSwitchOptionNode>)optionNodes,
            IsCheckpoint = isCheckpoint,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<LoopSwitchOptionNode>? ParseLoopSwitchOptions(ref int index)
    {
        ImmutableArray<LoopSwitchOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<LoopSwitchOptionNode>();

        while (tokens[index] is not { Kind: TokenKind.ClosedBrace })
        {
            long nodeIndex = tokens[index].Index;
            LoopSwitchOptionKind kind;

            switch (tokens[index])
            {
                case { Kind: TokenKind.OptionKeyword }:
                    kind = LoopSwitchOptionKind.None;
                    index++;
                    break;
                case { Kind: TokenKind.LoopKeyword }:
                    kind = LoopSwitchOptionKind.Loop;
                    index++;
                    _ = Expect(TokenKind.OptionKeyword, ref index);
                    break;
                case { Kind: TokenKind.FinalKeyword }:
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

        long nodeIndex = tokens[index].Index;
        index++;

        switch (tokens[index])
        {
            case { Kind: TokenKind.OutputKeyword }:
                OutputStatementNode? outputStatement = ParseOutputStatement(ref index, isCheckpoint: true);

                if (outputStatement is not null)
                {
                    outputStatement = outputStatement with
                    {
                        Index = nodeIndex,
                    };
                }

                return outputStatement;
            case { Kind: TokenKind.SwitchKeyword }:
                SwitchStatementNode? switchStatement = ParseSwitchStatement(ref index, isCheckpoint: true);

                if (switchStatement is not null)
                {
                    switchStatement = switchStatement with
                    {
                        Index = nodeIndex,
                    };
                }

                return switchStatement;
            case { Kind: TokenKind.LoopKeyword }:
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

        long nodeIndex = tokens[index].Index;
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

        while (tokens[index] is { Kind: TokenKind.OptionKeyword })
        {
            long nodeIndex = tokens[index].Index;

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

        if (tokens[index] is { Kind: TokenKind.OtherKeyword })
        {
            long nodeIndex = tokens[index].Index;

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
        Debug.Assert(tokens[index] is { Kind: TokenKind.CallKeyword });

        long nodeIndex = tokens[index].Index;
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
        Debug.Assert(tokens[index] is { Kind: TokenKind.Identifier });
        Token identifier = tokens[index];

        long nodeIndex = tokens[index].Index;
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
            Debug.Assert(tokens[index] is { Kind: TokenKind.StrengthenKeyword });
        }
        else
        {
            Debug.Assert(tokens[index] is { Kind: TokenKind.WeakenKeyword });
        }

        long nodeIndex = tokens[index].Index;
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
        long nodeIndex = tokens[index].Index;
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
