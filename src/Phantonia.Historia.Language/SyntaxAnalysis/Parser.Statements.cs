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
                return ParseOutputStatement(ref index);
            case { Kind: TokenKind.SwitchKeyword }:
                return ParseSwitchStatement(ref index);
            case { Kind: TokenKind.LoopKeyword }:
                return ParseLoopSwitchStatement(ref index);
            case { Kind: TokenKind.BranchOnKeyword }:
                return ParseBranchOnStatement(ref index);
            case { Kind: TokenKind.CallKeyword }:
                return ParseCallStatement(ref index);
            case { Kind: TokenKind.OutcomeKeyword }:
                {
                    (string name, ImmutableArray<string> options, string? defaultOption, int nodeIndex) = ParseOutcomeDeclaration(ref index);
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
                    (string name, ImmutableArray<SpectrumOptionNode> options, string? defaultOption, int nodeIndex) = ParseSpectrumDeclaration(ref index);
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

    private OutputStatementNode? ParseOutputStatement(ref int index)
    {
        Debug.Assert(tokens[index].Kind is TokenKind.OutputKeyword);

        int nodeIndex = tokens[index].Index;

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
            Index = nodeIndex,
        };
    }

    private SwitchStatementNode? ParseSwitchStatement(ref int index)
    {
        int nodeIndex = tokens[index].Index;

        Debug.Assert(tokens[index] is { Kind: TokenKind.SwitchKeyword });

        index++;

        string? name = null;

        if (tokens[index].Kind == TokenKind.Identifier)
        {
            name = tokens[index].Text;
            index++;
        }

        _ = Expect(TokenKind.OpenParenthesis, ref index);

        ExpressionNode? expression = ParseExpression(ref index);
        if (expression is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedParenthesis, ref index);

        _ = Expect(TokenKind.OpenBrace, ref index);

        ImmutableArray<SwitchOptionNode>? optionNodes = ParseSwitchOptions(ref index);
        if (optionNodes is null)
        {
            return null;
        }

        _ = Expect(TokenKind.ClosedBrace, ref index);

        return new SwitchStatementNode
        {
            Name = name,
            OutputExpression = expression,
            Options = (ImmutableArray<SwitchOptionNode>)optionNodes,
            Index = nodeIndex,
        };
    }

    private ImmutableArray<SwitchOptionNode>? ParseSwitchOptions(ref int index)
    {
        ImmutableArray<SwitchOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<SwitchOptionNode>();

        while (tokens[index] is { Kind: TokenKind.OptionKeyword })
        {
            int nodeIndex = tokens[index].Index;

            _ = Expect(TokenKind.OptionKeyword, ref index);

            string? name = null;

            if (tokens[index].Kind == TokenKind.Identifier)
            {
                name = tokens[index].Text;
                index++;
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

            SwitchOptionNode optionNode = new()
            {
                Name = name,
                Expression = expression,
                Body = body,
                Index = nodeIndex,
            };

            optionBuilder.Add(optionNode);
        }

        return optionBuilder.ToImmutable();
    }

    private LoopSwitchStatementNode? ParseLoopSwitchStatement(ref int index)
    {
        int nodeIndex = tokens[index].Index;

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
            Index = nodeIndex,
        };
    }

    private ImmutableArray<LoopSwitchOptionNode>? ParseLoopSwitchOptions(ref int index)
    {
        ImmutableArray<LoopSwitchOptionNode>.Builder optionBuilder = ImmutableArray.CreateBuilder<LoopSwitchOptionNode>();

        while (tokens[index] is not { Kind: TokenKind.ClosedBrace })
        {
            int nodeIndex = tokens[index].Index;
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

        while (tokens[index] is { Kind: TokenKind.OptionKeyword })
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

        if (tokens[index] is { Kind: TokenKind.OtherKeyword })
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
        Debug.Assert(tokens[index] is { Kind: TokenKind.CallKeyword });

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
        Debug.Assert(tokens[index] is { Kind: TokenKind.Identifier });
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
            Debug.Assert(tokens[index] is { Kind: TokenKind.StrengthenKeyword });
        }
        else
        {
            Debug.Assert(tokens[index] is { Kind: TokenKind.WeakenKeyword });
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
}
