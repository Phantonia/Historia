using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System;

namespace Phantonia.Historia.Language.SyntaxAnalysis;

public sealed class GlobalSyntaxAnalyzer(StoryNode story)
{
    public event Action<Error>? ErrorFound;

    public StoryNode Analyse()
    {
        foreach (TopLevelNode node in story.GetTopLevelNodes())
        {
            AnalyseTopLevelNode(node);
        }

        return story;
    }

    // if this were to ever rewrite, we can still change the return type
    private void AnalyseTopLevelNode(TopLevelNode node)
    {
        if (node is SubroutineSymbolDeclarationNode subroutineDeclaration)
        {
            SyntaxContext context = new();
            AnalyseStatementBody(subroutineDeclaration.Body, context);
        }
    }

    private void AnalyseStatementBody(StatementBodyNode body, SyntaxContext context)
    {
        foreach (StatementNode statement in body.Statements)
        {
            AnalyseStatement(statement, context);
        }
    }

    private void AnalyseStatement(StatementNode statement, SyntaxContext context)
    {
        switch (statement)
        {
            case ContinueStatementNode:
                if (!context.IsInSwitchOption)
                {
                    ErrorFound?.Invoke(Errors.ContinueOutsideOfSwitchOption(statement.Index));
                }

                break;
            case SwitchStatementNode switchStatement:
                if (context.IsInSwitchBody)
                {
                    ErrorFound?.Invoke(Errors.SwitchBodyContainsSwitchOrCall(switchStatement.Index));
                }

                AnalyseStatementBody(switchStatement.Body, context with { IsInSwitchBody = true });

                foreach (OptionNode option in switchStatement.Options)
                {
                    AnalyseStatementBody(option.Body, context with { IsInSwitchOption = true });
                }

                break;
            case BranchOnStatementNode branchonStatement:
                foreach (BranchOnOptionNode option in branchonStatement.Options)
                {
                    AnalyseStatementBody(option.Body, context);
                }

                break;
            case LoopSwitchStatementNode loopSwitchStatement:
                if (context.IsInSwitchBody)
                {
                    ErrorFound?.Invoke(Errors.SwitchBodyContainsSwitchOrCall(loopSwitchStatement.Index));
                }
                
                foreach (LoopSwitchOptionNode option in loopSwitchStatement.Options)
                {
                    AnalyseStatementBody(option.Body, context with { IsInLoopSwitchOption = true });
                }

                break;
            case IfStatementNode ifStatement:
                AnalyseStatementBody(ifStatement.ThenBlock, context);

                if (ifStatement.ElseBlock is not null)
                {
                    AnalyseStatementBody(ifStatement.ElseBlock, context);
                }

                break;
            case ChooseStatementNode chooseStatement:
                foreach (OptionNode option in chooseStatement.Options)
                {
                    AnalyseStatementBody(option.Body, context);
                }

                break;
            case CallStatementNode:
                if (context.IsInSwitchBody)
                {
                    ErrorFound?.Invoke(Errors.SwitchBodyContainsSwitchOrCall(statement.Index));
                }

                break;
        }
    }

    private readonly record struct SyntaxContext(bool IsInSwitchOption, bool IsInSwitchBody, bool IsInLoopSwitchOption);
}
