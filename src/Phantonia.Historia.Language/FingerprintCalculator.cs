using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System;
using System.Linq;

namespace Phantonia.Historia.Language;

public sealed class FingerprintCalculator(StoryNode boundStory, SymbolTable symbolTable)
{
    public ulong GetStoryFingerprint()
    {
        return Fingerprinting.Combine(boundStory.CompilationUnits.Select(GetUnitFingerprint));
    }

    private ulong GetUnitFingerprint(CompilationUnitNode unit)
    {
        ulong pathFingerprint = Fingerprinting.HashString(unit.Path);
        ulong nodesFingerprint = Fingerprinting.Combine(unit.TopLevelNodes.Select(GetTopLevelNodeFingerprint));
        return Fingerprinting.Combine(pathFingerprint, nodesFingerprint);
    }

    private ulong GetTopLevelNodeFingerprint(TopLevelNode node)
    {
        return node switch
        {
            BoundSymbolDeclarationNode { Original: OutcomeSymbolDeclarationNode outcomeDeclaration } => Fingerprinting.Jumble((ulong)outcomeDeclaration.Options.Length),
            BoundSymbolDeclarationNode { Original: SpectrumSymbolDeclarationNode } => 543309055156133753,
            BoundSymbolDeclarationNode { Original: SubroutineSymbolDeclarationNode subDeclaration } => GetBodyFingerprint(subDeclaration.Body),
            _ => 0,
        };
    }

    private ulong GetBodyFingerprint(StatementBodyNode body)
    {
        ulong fingerprint = 436220662013894249;

        foreach (StatementNode statement in body.Statements)
        {
            switch (statement)
            {
                case OutcomeDeclarationStatementNode outcomeDeclaration:
                    fingerprint = Fingerprinting.Combine(fingerprint, (ulong)outcomeDeclaration.Options.Length);
                    break;
                case SpectrumDeclarationStatementNode:
                    fingerprint = Fingerprinting.Combine(fingerprint, 635591167028209507);
                    break;
                case LoopSwitchStatementNode loopSwitchStatement:
                    fingerprint = Fingerprinting.Combine(fingerprint, 679904703702922241);

                    foreach (LoopSwitchOptionNode option in loopSwitchStatement.Options)
                    {
                        fingerprint = Fingerprinting.Combine(fingerprint, (ulong)option.Kind, GetBodyFingerprint(option.Body));
                    }

                    break;
                case SwitchStatementNode switchStatement:
                    foreach (OptionNode option in switchStatement.Options)
                    {
                        fingerprint = Fingerprinting.Combine(fingerprint, GetBodyFingerprint(option.Body));
                    }

                    break;
                case BranchOnStatementNode branchonStatement:
                    foreach (BranchOnOptionNode option in branchonStatement.Options)
                    {
                        fingerprint = Fingerprinting.Combine(fingerprint, GetBodyFingerprint(option.Body));
                    }

                    break;
                case ChooseStatementNode chooseStatement:
                    foreach (OptionNode option in chooseStatement.Options)
                    {
                        fingerprint = Fingerprinting.Combine(fingerprint, GetBodyFingerprint(option.Body));
                    }

                    break;
                case IfStatementNode ifStatement:
                    fingerprint = Fingerprinting.Combine(fingerprint, GetBodyFingerprint(ifStatement.ThenBlock));

                    if (ifStatement.ElseBlock is not null)
                    {
                        fingerprint = Fingerprinting.Combine(fingerprint, GetBodyFingerprint(ifStatement.ElseBlock));
                    }

                    break;
                case BoundCallStatementNode callStatement:
                    fingerprint = Fingerprinting.Combine(fingerprint, GetSceneFingerprint(callStatement.Subroutine));
                    break;
            }
        }

        return fingerprint;
    }

    private ulong GetSceneFingerprint(SubroutineSymbol subroutine)
    {
        SubroutineSymbol[] allSubroutines =
            symbolTable.AllSymbols
                       .OfType<SubroutineSymbol>()
                       .OrderBy(s => s.Index)
                       .ToArray();

        int index = Array.IndexOf(allSubroutines, subroutine);

        return Fingerprinting.Jumble((ulong)index);
    }
}
