﻿using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using Phantonia.Historia.Language.SyntaxAnalysis.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.SemanticAnalysis;

public sealed partial class Binder
{
    private (SymbolTable, Settings, StoryNode) BindSettingDirectives(StoryNode halfboundStory, SymbolTable table)
    {
        List<TopLevelNode> topLevelNodes = halfboundStory.TopLevelNodes.ToList();

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

        halfboundStory = halfboundStory with { TopLevelNodes = topLevelNodes.ToImmutableArray() };

        Settings settings = new();

        foreach (SettingDirectiveNode directive in topLevelNodes.OfType<SettingDirectiveNode>())
        {
            switch (directive)
            {
                case TypeSettingDirectiveNode
                {
                    SettingName: nameof(Settings.OutputType),
                    Type: BoundTypeNode { Symbol: TypeSymbol outputType }
                }:
                    settings = settings with { OutputType = outputType };
                    break;
                case TypeSettingDirectiveNode
                {
                    SettingName: nameof(Settings.OptionType),
                    Type: BoundTypeNode { Symbol: TypeSymbol optionType }
                }:
                    settings = settings with { OptionType = optionType };
                    break;
            }
        }

        return (table, settings, halfboundStory);
    }

    private (SymbolTable, StoryNode) BindTree(StoryNode halfboundStory, Settings settings, SymbolTable table)
    {
        List<TopLevelNode> topLevelNodes = halfboundStory.TopLevelNodes.ToList();

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

        StoryNode boundStory = halfboundStory with { TopLevelNodes = topLevelNodes.ToImmutableArray() };
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

                    BoundTypeNode boundType = new() { Node = type, Symbol = typeSymbol, Index = type.Index };
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
}
