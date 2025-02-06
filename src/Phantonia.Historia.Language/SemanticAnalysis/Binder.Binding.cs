using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (SymbolTable, StoryNode) BindTree(StoryNode halfboundStory, Settings settings, SymbolTable table)
    {
        BindingContext context = new()
        {
            SymbolTable = table,
        };

        List<CompilationUnitNode> compilationUnits = [.. halfboundStory.CompilationUnits];

        for (int i = 0; i < compilationUnits.Count; i++)
        {
            CompilationUnitNode compilationUnit = compilationUnits[i];

            List<TopLevelNode> topLevelNodes = [.. compilationUnit.TopLevelNodes];

            for (int j = 0; j < topLevelNodes.Count; j++)
            {
                if (topLevelNodes[j] is TypeSymbolDeclarationNode or SettingDirectiveNode)
                {
                    // already bound these
                    continue;
                }

                (context, TopLevelNode boundDeclaration) = BindTopLevelNode(topLevelNodes[j], settings, context);
                topLevelNodes[j] = boundDeclaration;
            }

            compilationUnits[i] = compilationUnits[i] with
            {
                TopLevelNodes = [.. topLevelNodes],
            };
        }

        StoryNode boundStory = halfboundStory with
        {
            CompilationUnits = [.. compilationUnits],
        };

        return (context.SymbolTable, boundStory);
    }

    private (BindingContext, TypeNode) BindType(TypeNode type, BindingContext context)
    {
        switch (type)
        {
            case IdentifierTypeNode identifierType:
                {
                    if (!context.SymbolTable.IsDeclared(identifierType.Identifier))
                    {
                        ErrorFound?.Invoke(Errors.SymbolDoesNotExistInScope(identifierType.Identifier, identifierType.Index));
                        return (context, type);
                    }

                    Symbol symbol = context.SymbolTable[identifierType.Identifier];

                    if (symbol is not TypeSymbol typeSymbol)
                    {
                        ErrorFound?.Invoke(Errors.NonTypeSymbolUsedAsType(identifierType.Identifier, identifierType.Index));
                        return (context, type);
                    }

                    BoundTypeNode boundType = new()
                    {
                        Original = type,
                        Symbol = typeSymbol,
                        Index = type.Index,
                        PrecedingTokens = [],
                    };

                    return (context, boundType);
                }
            default:
                Debug.Assert(false);
                return default;
        }
    }

    private (BindingContext, OutcomeSymbol?) BindOutcomeDeclaration(IOutcomeDeclarationNode outcomeDeclaration, BindingContext context)
    {
        bool error = false;

        if (context.SymbolTable.IsDeclared(outcomeDeclaration.Name))
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
            return (context, null);
        }

        context = context with
        {
            SymbolTable = context.SymbolTable.Declare(symbol),
        };

        return (context, symbol);
    }

    private (BindingContext, SpectrumSymbol?) BindSpectrumDeclaration(ISpectrumDeclarationNode spectrumDeclaration, BindingContext context)
    {
        bool error = false;

        if (context.SymbolTable.IsDeclared(spectrumDeclaration.Name))
        {
            ErrorFound?.Invoke(Errors.DuplicatedSymbolName(spectrumDeclaration.Name, spectrumDeclaration.Index));
            error = true;
        }

        SpectrumSymbol? symbol = CreateSpectrumSymbolFromDeclaration(spectrumDeclaration);

        if (symbol is null || error)
        {
            return (context, null);
        }

        context = context with
        {
            SymbolTable = context.SymbolTable.Declare(symbol),
        };

        return (context, symbol);
    }

    private (BindingContext, List<ArgumentNode>) BindArgumentList(IArgumentContainerNode argumentContainer, BindingContext context, IReadOnlyList<PropertySymbol> properties, string parameterOrProperty)
    {
        List<ArgumentNode> boundArguments = [.. argumentContainer.Arguments];

        for (int i = 0; i < argumentContainer.Arguments.Length; i++)
        {
            if (argumentContainer.Arguments[i].ParameterName != null && argumentContainer.Arguments[i].ParameterName != properties[i].Name)
            {
                ErrorFound?.Invoke(Errors.WrongPropertyInRecordCreation(argumentContainer.Arguments[i].ParameterName!, argumentContainer.Arguments[i].Index));

                continue;
            }

            TypeSymbol propertyType = properties[i].Type;

            (context, ExpressionNode maybeTypedExpression) = BindAndTypeExpression(argumentContainer.Arguments[i].Expression, context);

            if (maybeTypedExpression is not TypedExpressionNode typedExpression)
            {
                continue;
            }

            if (!TypesAreCompatible(typedExpression.SourceType, propertyType))
            {
                ErrorFound?.Invoke(Errors.IncompatibleType(typedExpression.SourceType, propertyType, parameterOrProperty, argumentContainer.Arguments[i].Index));
                continue;
            }

            typedExpression = RecursivelySetTargetType(typedExpression, properties[i].Type);

            BoundArgumentNode boundArgument = new()
            {
                ParameterNameToken = argumentContainer.Arguments[i].ParameterNameToken,
                EqualsToken = argumentContainer.Arguments[i].EqualsToken,
                Expression = typedExpression,
                CommaToken = argumentContainer.Arguments[i].CommaToken,
                Property = properties[i],
                Index = argumentContainer.Index,
                PrecedingTokens = [],
            };

            boundArguments[i] = boundArgument;
        }

        return (context, boundArguments);
    }
}
