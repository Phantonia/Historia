using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;
using System.Diagnostics;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (SymbolTable, StoryNode) BindTree(StoryNode halfboundStory, Settings settings, SymbolTable table)
    {
        List<TopLevelNode> topLevelNodes = [.. halfboundStory.TopLevelNodes];

        for (int i = 0; i < topLevelNodes.Count; i++)
        {
            TopLevelNode topLevelNode = topLevelNodes[i];

            if (topLevelNode is TypeSymbolDeclarationNode or SettingDirectiveNode)
            {
                // already bound these
                continue;
            }

            (table, TopLevelNode boundDeclaration) = BindTopLevelNode(topLevelNode, settings, table);
            topLevelNodes[i] = boundDeclaration;
        }

        StoryNode boundStory = halfboundStory with { TopLevelNodes = [.. topLevelNodes] };
        return (table, boundStory);
    }

    private (SymbolTable, TypeNode) BindType(TypeNode type, SymbolTable table)
    {
        switch (type)
        {
            case IdentifierTypeNode identifierType:
                {
                    if (!table.IsDeclared(identifierType.Identifier))
                    {
                        ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(identifierType.Identifier, identifierType.Index));
                        return (table, type);
                    }

                    Symbol symbol = table[identifierType.Identifier];

                    if (symbol is not TypeSymbol typeSymbol)
                    {
                        ErrorFound?.Invoke(Errors.NonTypeSymbolUsedAsType(identifierType.Identifier, identifierType.Index));
                        return (table, type);
                    }

                    BoundTypeNode boundType = new() { Original = type, Symbol = typeSymbol, Index = type.Index };
                    return (table, boundType);
                }
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (SymbolTable, OutcomeSymbol?) BindOutcomeDeclaration(IOutcomeDeclarationNode outcomeDeclaration, SymbolTable table)
    {
        bool error = false;

        if (table.IsDeclared(outcomeDeclaration.Name))
        {
            ErrorFound?.Invoke(Errors.DuplicatedSymbolName(outcomeDeclaration.Name, outcomeDeclaration.Index));
            error = true;
        }

        if (outcomeDeclaration.Options.Length == 0)
        {
            ErrorFound?.Invoke(Errors.OutcomeWithZeroOptions(outcomeDeclaration.Name, outcomeDeclaration.Index));
            error = true;
        }

        OutcomeSymbol? symbol = CreateOutcomeSymbolFromDeclaration(outcomeDeclaration);

        if (symbol is null || error)
        {
            return (table, null);
        }

        table = table.Declare(symbol);

        return (table, symbol);
    }

    private (SymbolTable, SpectrumSymbol?) BindSpectrumDeclaration(ISpectrumDeclarationNode spectrumDeclaration, SymbolTable table)
    {
        bool error = false;

        if (table.IsDeclared(spectrumDeclaration.Name))
        {
            ErrorFound?.Invoke(Errors.DuplicatedSymbolName(spectrumDeclaration.Name, spectrumDeclaration.Index));
            error = true;
        }

        SpectrumSymbol? symbol = CreateSpectrumSymbolFromDeclaration(spectrumDeclaration);

        if (symbol is null || error)
        {
            return (table, null);
        }

        table = table.Declare(symbol);

        return (table, symbol);
    }

    private (SymbolTable, List<ArgumentNode>) BindArgumentList(IArgumentContainerNode argumentContainer, SymbolTable table, IReadOnlyList<PropertySymbol> properties, string parameterOrProperty)
    {
        List<ArgumentNode> boundArguments = [.. argumentContainer.Arguments];

        for (int i = 0; i < argumentContainer.Arguments.Length; i++)
        {
            if (argumentContainer.Arguments[i].PropertyName != null && argumentContainer.Arguments[i].PropertyName != properties[i].Name)
            {
                ErrorFound?.Invoke(Errors.WrongPropertyInRecordCreation(argumentContainer.Arguments[i].PropertyName!, argumentContainer.Arguments[i].Index));

                continue;
            }

            TypeSymbol propertyType = properties[i].Type;

            (table, ExpressionNode maybeTypedExpression) = BindAndTypeExpression(argumentContainer.Arguments[i].Expression, table);

            if (maybeTypedExpression is not TypedExpressionNode typedExpression)
            {
                continue;
            }

            if (!TypesAreCompatible(typedExpression.SourceType, propertyType))
            {
                ErrorFound?.Invoke(Errors.IncompatibleType(typedExpression.SourceType, propertyType, parameterOrProperty, argumentContainer.Arguments[i].Index));
                continue;
            }

            typedExpression = typedExpression with
            {
                TargetType = properties[i].Type,
            };

            BoundArgumentNode boundArgument = new()
            {
                Expression = typedExpression,
                PropertyName = argumentContainer.Arguments[i].PropertyName,
                Property = properties[i],
                Index = argumentContainer.Index,
            };

            boundArguments[i] = boundArgument;
        }

        return (table, boundArguments);
    }
}
