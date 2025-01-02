using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (SymbolTable, Settings, StoryNode) BindSettingDirectives(StoryNode halfboundStory, SymbolTable table)
    {
        List<TopLevelNode> topLevelNodes = [.. halfboundStory.TopLevelNodes];

        for (int i = 0; i < topLevelNodes.Count; i++)
        {
            TopLevelNode topLevelNode = topLevelNodes[i];

            if (topLevelNode is not SettingDirectiveNode directive)
            {
                // already bound these
                continue;
            }

            (table, SettingDirectiveNode boundDirective) = BindSingleSettingDirective(directive, table);
            topLevelNodes[i] = boundDirective;
        }

        halfboundStory = halfboundStory with { TopLevelNodes = [.. topLevelNodes] };

        Settings settings = new();

        foreach (SettingDirectiveNode directive in topLevelNodes.OfType<SettingDirectiveNode>())
        {
            settings = EvaluateSettingDirective(directive, settings, table);
        }

        if (table.IsDeclared(settings.StoryName))
        {
            Symbol conflictingSymbol = table[settings.StoryName];

            if (conflictingSymbol is TypeSymbol)
            {
                long index = story.TopLevelNodes.OfType<ExpressionSettingDirectiveNode>().SingleOrDefault(t => t.SettingName == nameof(Settings.StoryName))?.Expression?.Index ?? 0;

                ErrorFound?.Invoke(Errors.ConflictingStoryName(settings.StoryName, index));
            }
        }

        return (table, settings, halfboundStory);
    }

    private (SymbolTable, SettingDirectiveNode) BindSingleSettingDirective(SettingDirectiveNode directive, SymbolTable table)
    {
        switch (directive)
        {
            case TypeSettingDirectiveNode typeSetting:
                (table, TypeNode boundType) = BindType(typeSetting.Type, table);
                return (table, typeSetting with { Type = boundType });
            case ExpressionSettingDirectiveNode expressionSetting:
                (table, ExpressionNode boundExpression) = BindAndTypeExpression(expressionSetting.Expression, table);
                return (table, expressionSetting with { Expression = boundExpression });
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
                    Expression: ExpressionNode expressionNode,
                }
            }:
                return EvaluateNamespaceSetting(expressionNode, expressionType, previousSettings, table);
            case ExpressionSettingDirectiveNode
            {
                SettingName: nameof(Settings.StoryName),
                Expression: TypedExpressionNode
                {
                    SourceType: TypeSymbol expressionType,
                    Expression: ExpressionNode expressionNode,
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

        string className = ((StringLiteralExpressionNode)expressionNode).StringLiteral.Trim();

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

        string namespaceString = ((StringLiteralExpressionNode)expressionNode).StringLiteral;
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