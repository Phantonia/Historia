using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using static Phantonia.Historia.Language.Fingerprinting;

namespace Phantonia.Historia.Language;

public static class FingerprintCalculator
{
    public static ulong GetStoryFingerprint(StoryNode story)
    {
        return Combine(story.CompilationUnits.Select(GetUnitFingerprint));
    }

    private static ulong GetUnitFingerprint(CompilationUnitNode unit)
    {
        ulong pathFingerprint = HashString(unit.Path);
        ulong nodesFingerprint = Combine(unit.TopLevelNodes.Select(GetTopLevelNodeFingerprint));
        return Combine(pathFingerprint, nodesFingerprint);
    }

    private static ulong GetTopLevelNodeFingerprint(TopLevelNode node)
    {
        return node switch
        {
            OutcomeSymbolDeclarationNode outcomeDeclaration => GetOutcomeDeclarationFingerprint(outcomeDeclaration),
            SpectrumSymbolDeclarationNode spectrumDeclaration => GetSpectrumDeclarationFingerprint(spectrumDeclaration),
            SubroutineSymbolDeclarationNode subDeclaration => GetSubroutineDeclarationFingerprint(subDeclaration),
            RecordSymbolDeclarationNode recordDeclaration => GetRecordDeclarationFingerprint(recordDeclaration),
            EnumSymbolDeclarationNode enumDeclaration => GetEnumDeclarationFingerprint(enumDeclaration),
            UnionSymbolDeclarationNode unionDeclaration => GetUnionDeclarationFingerprint(unionDeclaration),
            InterfaceSymbolDeclarationNode interfaceDeclaration => GetInterfaceDeclarationFingerprint(interfaceDeclaration),
            ReferenceSymbolDeclarationNode referenceDeclaration => GetReferenceDeclarationFingerprint(referenceDeclaration),
            SettingDirectiveNode settingDirective => GetSettingDirectiveFingerprint(settingDirective),
            _ => throw new InvalidOperationException("Unknown node type"),
        };
    }

    private static ulong GetReferenceDeclarationFingerprint(ReferenceSymbolDeclarationNode referenceDeclaration)
    {
        return Combine(329611647455711521, HashString(referenceDeclaration.Name), HashString(referenceDeclaration.InterfaceName));
    }

    private static ulong GetInterfaceDeclarationFingerprint(InterfaceSymbolDeclarationNode interfaceDeclaration)
    {
        ulong fingerprint = Combine(544296832459018817, HashString(interfaceDeclaration.Name));

        foreach (InterfaceMethodDeclarationNode method in interfaceDeclaration.Methods)
        {
            fingerprint = Combine(fingerprint, (ulong)method.Kind, HashString(method.Name));

            foreach (ParameterDeclarationNode parameter in method.Parameters)
            {
                fingerprint = Combine(fingerprint, HashString(parameter.Name), GetTypeFingerprint(parameter.Type));
            }
        }

        return fingerprint;
    }

    private static ulong GetUnionDeclarationFingerprint(UnionSymbolDeclarationNode unionDeclaration)
    {
        ulong fingerprint = Combine(724149214827331879, HashString(unionDeclaration.Name));

        foreach (TypeNode type in unionDeclaration.Subtypes)
        {
            fingerprint = Combine(fingerprint, GetTypeFingerprint(type));
        }

        return fingerprint;
    }

    private static ulong GetEnumDeclarationFingerprint(EnumSymbolDeclarationNode enumDeclaration)
    {
        ulong fingerprint = Combine(439871096558495999, HashString(enumDeclaration.Name));

        foreach (string option in enumDeclaration.Options)
        {
            fingerprint = Combine(fingerprint, HashString(option));
        }

        return fingerprint;
    }

    private static ulong GetRecordDeclarationFingerprint(RecordSymbolDeclarationNode recordDeclaration)
    {
        ulong fingerprint = Combine(175242444133064921, HashString(recordDeclaration.Name));

        foreach (ParameterDeclarationNode property in recordDeclaration.Properties)
        {
            fingerprint = Combine(fingerprint, HashString(property.Name), GetTypeFingerprint(property.Type));
        }

        return fingerprint;
    }

    private static ulong GetSettingDirectiveFingerprint(SettingDirectiveNode settingDirective)
    {
        if (settingDirective is TypeSettingDirectiveNode { Type: TypeNode type })
        {
            return Combine(490822851885444649, HashString(settingDirective.SettingName), GetTypeFingerprint(type));
        }
        else if (settingDirective is ExpressionSettingDirectiveNode { Expression: ExpressionNode expression })
        {
            return Combine(453453548555170351, HashString(settingDirective.SettingName));
        }

        throw new InvalidOperationException("Invalid type of setting directive");
    }

    private static ulong GetSubroutineDeclarationFingerprint(SubroutineSymbolDeclarationNode subDeclaration)
    {
        return Combine((ulong)subDeclaration.DeclaratorToken.Kind, GetBodyFingerprint(subDeclaration.Body));
    }

    private static ulong GetBodyFingerprint(StatementBodyNode body)
    {
        ulong fingerprint = 436220662013894249;

        foreach (StatementNode statement in body.Statements)
        {
            switch (statement)
            {
                case OutputStatementNode:
                case LineStatementNode:
                    fingerprint = Combine(fingerprint, 260881577110066963);
                    break;
                case OutcomeDeclarationStatementNode outcomeDeclaration:
                    fingerprint = Combine(fingerprint, GetOutcomeDeclarationFingerprint(outcomeDeclaration));
                    break;
                case SpectrumDeclarationStatementNode spectrumDeclaration:
                    fingerprint = Combine(fingerprint, GetSpectrumDeclarationFingerprint(spectrumDeclaration));
                    break;
                case LoopSwitchStatementNode loopSwitchStatement:
                    fingerprint = Combine(fingerprint, 679904703702922241);

                    foreach (LoopSwitchOptionNode option in loopSwitchStatement.Options)
                    {
                        fingerprint = Combine(fingerprint, (ulong)option.Kind, GetBodyFingerprint(option.Body));
                    }

                    break;
                case SwitchStatementNode switchStatement:
                    fingerprint = Combine(fingerprint, 589423032783369979);

                    foreach (OptionNode option in switchStatement.Options)
                    {
                        fingerprint = Combine(fingerprint, GetBodyFingerprint(option.Body));
                    }

                    break;
                case BranchOnStatementNode branchonStatement:
                    fingerprint = Combine(fingerprint, 359975195009582093);

                    foreach (BranchOnOptionNode option in branchonStatement.Options)
                    {
                        fingerprint = Combine(fingerprint, GetBodyFingerprint(option.Body));
                    }

                    break;
                case ChooseStatementNode chooseStatement:
                    fingerprint = Combine(fingerprint, 281882467342145861);

                    foreach (OptionNode option in chooseStatement.Options)
                    {
                        fingerprint = Combine(fingerprint, GetBodyFingerprint(option.Body));
                    }

                    break;
                case IfStatementNode ifStatement:
                    fingerprint = Combine(fingerprint, 390213269020147271, GetBodyFingerprint(ifStatement.ThenBlock));

                    if (ifStatement.ElseBlock is not null)
                    {
                        fingerprint = Combine(fingerprint, GetBodyFingerprint(ifStatement.ElseBlock));
                    }

                    break;
                case BoundCallStatementNode callStatement:
                    fingerprint = Combine(fingerprint, 420327632913748891, HashString(callStatement.SubroutineName));
                    break;
                case RunStatementNode:
                    fingerprint = Combine(fingerprint, 670637564060611267);
                    break;
                case AssignmentStatementNode assignmentStatement:
                    fingerprint = Combine(fingerprint, 385684897427296031, HashString(assignmentStatement.VariableName), GetExpressionFingerprint(assignmentStatement.AssignedExpression));
                    break;
                case SpectrumAdjustmentStatementNode adjustmentStatement:
                    fingerprint = Combine(fingerprint, (ulong)adjustmentStatement.StrengthenOrWeakenKeywordToken.Kind, HashString(adjustmentStatement.SpectrumName), GetExpressionFingerprint(adjustmentStatement.AdjustmentAmount));
                    break;
            }
        }

        return fingerprint;
    }

    private static ulong GetOutcomeDeclarationFingerprint(IOutcomeDeclarationNode outcomeDeclaration)
    {
        ulong fingerprint = HashString(outcomeDeclaration.Name);

        foreach (string option in outcomeDeclaration.Options)
        {
            fingerprint = Combine(fingerprint, HashString(option));
        }

        if (outcomeDeclaration.DefaultOption is not null)
        {
            fingerprint = Combine(fingerprint, HashString(outcomeDeclaration.DefaultOption));
        }

        return fingerprint;
    }

    private static ulong GetSpectrumDeclarationFingerprint(ISpectrumDeclarationNode spectrumDeclaration)
    {
        ulong fingerprint = HashString(spectrumDeclaration.Name);

        foreach (SpectrumOptionNode option in spectrumDeclaration.Options)
        {
            fingerprint = Combine(fingerprint, HashString(option.Name), (ulong)option.Numerator, (ulong)option.Denominator);
        }

        if (spectrumDeclaration.DefaultOption is not null)
        {
            fingerprint = Combine(fingerprint, HashString(spectrumDeclaration.DefaultOption));
        }

        return fingerprint;
    }

    private static ulong GetExpressionFingerprint(ExpressionNode expression)
    {
        // this method is only really needed for assignment expressions

        if (expression is IdentifierExpressionNode { Identifier: string identifier })
        {
            return HashString(identifier);
        }

        if (expression is IntegerLiteralExpressionNode { Value: int value })
        {
            return Jumble(Unsafe.BitCast<int, uint>(value));
        }

        return 658437956571930839;
    }

    private static ulong GetTypeFingerprint(TypeNode type)
    {
        Debug.Assert(type is IdentifierTypeNode);

        return HashString(((IdentifierTypeNode)type).Identifier);
    }
}
