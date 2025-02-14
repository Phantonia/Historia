using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (SymbolTable, Settings, StoryNode) BindSettingDirectives(StoryNode halfboundStory, SymbolTable table)
    {
        List<CompilationUnitNode> compilationUnits = [.. halfboundStory.CompilationUnits];

        for (int i = 0; i < compilationUnits.Count; i++)
        {
            List<TopLevelNode> topLevelNodes = [.. compilationUnits[i].TopLevelNodes];

            for (int j = 0; j < topLevelNodes.Count; j++)
            {
                TopLevelNode topLevelNode = topLevelNodes[j];

                if (topLevelNode is not SettingDirectiveNode directive)
                {
                    // already bound these
                    continue;
                }

                (table, SettingDirectiveNode boundDirective) = BindSingleSettingDirective(directive, table);
                topLevelNodes[j] = boundDirective;
            }

            compilationUnits[i] = compilationUnits[i] with
            {
                TopLevelNodes = [.. topLevelNodes],
            };
        }

        halfboundStory = halfboundStory with { CompilationUnits = [.. compilationUnits] };

        Settings settings = new();

        foreach (SettingDirectiveNode directive in halfboundStory.GetTopLevelNodes().OfType<SettingDirectiveNode>())
        {
            settings = EvaluateSettingDirective(directive, settings, table);
        }

        if (table.IsDeclared(settings.StoryName))
        {
            Symbol conflictingSymbol = table[settings.StoryName];

            if (conflictingSymbol is TypeSymbol)
            {
                long index = story.GetTopLevelNodes().OfType<ExpressionSettingDirectiveNode>().SingleOrDefault(t => t.SettingName == nameof(Settings.StoryName))?.Expression?.Index ?? 0;

                ErrorFound?.Invoke(Errors.ConflictingStoryName(settings.StoryName, index));
            }
        }

        return (table, settings, halfboundStory);
    }

    private (SymbolTable, SettingDirectiveNode) BindSingleSettingDirective(SettingDirectiveNode directive, SymbolTable table)
    {
        BindingContext context = new()
        {
            SymbolTable = table,
        };

        switch (directive)
        {
            case TypeSettingDirectiveNode typeSetting:
                (context, TypeNode boundType) = BindType(typeSetting.Type, context);
                return (context.SymbolTable, typeSetting with { Type = boundType });
            case ExpressionSettingDirectiveNode expressionSetting:
                (context, ExpressionNode boundExpression) = BindAndTypeExpression(expressionSetting.Expression, context);
                return (context.SymbolTable, expressionSetting with { Expression = boundExpression });
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private Settings EvaluateSettingDirective(SettingDirectiveNode directive, Settings previousSettings, SymbolTable table)
    {
        switch (directive)
        {
            case TypeSettingDirectiveNode
            {
                SettingName: nameof(Settings.OutputType),
                Type: BoundTypeNode { Symbol: TypeSymbol outputType }
            }:
                return previousSettings with
                {
                    OutputType = outputType,
                };
            case TypeSettingDirectiveNode
            {
                SettingName: nameof(Settings.OptionType),
                Type: BoundTypeNode { Symbol: TypeSymbol optionType }
            }:
                return previousSettings with
                {
                    OptionType = optionType,
                };
            case ExpressionSettingDirectiveNode
            {
                SettingName: nameof(Settings.Namespace),
                Expression: TypedExpressionNode
                {
                    SourceType: TypeSymbol expressionType,
                    Original: ExpressionNode expressionNode,
                }
            }:
                return EvaluateNamespaceSetting(expressionNode, expressionType, previousSettings, table);
            case ExpressionSettingDirectiveNode
            {
                SettingName: nameof(Settings.StoryName),
                Expression: TypedExpressionNode
                {
                    SourceType: TypeSymbol expressionType,
                    Original: ExpressionNode expressionNode,
                }
            }:
                return EvaluateStoryNameSetting(expressionNode, expressionType, previousSettings, table);
            default:
                Debug.Assert(false);
                return null;
        }
    }

    private Settings EvaluateStoryNameSetting(ExpressionNode expressionNode, TypeSymbol expressionType, Settings previousSettings, SymbolTable table)
    {
        if (!TypesAreCompatible(expressionType, (TypeSymbol)table["String"]))
        {
            ErrorFound?.Invoke(Errors.IncompatibleType(expressionType, (TypeSymbol)table["String"], "setting", expressionNode.Index));
            return previousSettings;
        }

        if (expressionNode is not StringLiteralExpressionNode literalExpression)
        {
            ErrorFound?.Invoke(Errors.SettingRequiresStringLiteral("StoryName", expressionNode.Index));
            return previousSettings;
        }

        string className = literalExpression.StringLiteral.Trim();

        Regex identifierRegex = IdentifierRegex();

        if (!identifierRegex.IsMatch(className))
        {
            ErrorFound?.Invoke(Errors.InvalidStoryName(className, expressionNode.Index));
            return previousSettings;
        }

        return previousSettings with
        {
            StoryName = className,
        };
    }

    private Settings EvaluateNamespaceSetting(ExpressionNode expressionNode, TypeSymbol expressionType, Settings previousSettings, SymbolTable table)
    {
        if (!TypesAreCompatible(expressionType, (TypeSymbol)table["String"]))
        {
            ErrorFound?.Invoke(Errors.IncompatibleType(expressionType, (TypeSymbol)table["String"], "setting", expressionNode.Index));
            return previousSettings;
        }

        if (expressionNode is not StringLiteralExpressionNode literalExpression)
        {
            ErrorFound?.Invoke(Errors.SettingRequiresStringLiteral("Namespace", expressionNode.Index));
            return previousSettings;
        }

        string namespaceString = literalExpression.StringLiteral;
        string[] components = namespaceString.Split('.', StringSplitOptions.TrimEntries);

        Regex identifierRegex = IdentifierRegex();

        if (!components.All(c => identifierRegex.IsMatch(c)))
        {
            ErrorFound?.Invoke(Errors.InvalidNamespaceFormat(namespaceString, expressionNode.Index));
            return previousSettings;
        }

        if (components[0] is nameof(System) or nameof(Microsoft) or nameof(Phantonia))
        {
            ErrorFound?.Invoke(Errors.ForbiddenNamespace(namespaceString, expressionNode.Index));
            return previousSettings;
        }

        return previousSettings with
        {
            Namespace = string.Join('.', components),
        };
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Singleline)]
    private static partial Regex IdentifierRegex();
}