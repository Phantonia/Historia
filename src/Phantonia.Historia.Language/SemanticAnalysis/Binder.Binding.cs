using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
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

        HashSet<string> optionNames = new();

        foreach (string option in outcomeDeclaration.Options)
        {
            if (!optionNames.Add(option))
            {
                ErrorFound?.Invoke(Errors.DuplicatedOptionInOutcomeDeclaration(option, outcomeDeclaration.Index));
                error = true;
            }
        }

        if (outcomeDeclaration.DefaultOption is not null && !optionNames.Contains(outcomeDeclaration.DefaultOption))
        {
            ErrorFound?.Invoke(Errors.OutcomeDefaultOptionNotAnOption(outcomeDeclaration.Name, outcomeDeclaration.Index));
            error = true;
        }

        if (error)
        {
            return (table, null);
        }

        OutcomeSymbol symbol = new()
        {
            Name = outcomeDeclaration.Name,
            OptionNames = optionNames.ToImmutableArray(),
            DefaultOption = outcomeDeclaration.DefaultOption,
            Index = outcomeDeclaration.Index,
        };

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

        if (spectrumDeclaration.Options.Length == 0)
        {
            ErrorFound?.Invoke(Errors.OutcomeWithZeroOptions(spectrumDeclaration.Name, spectrumDeclaration.Index));
            error = true;
        }

        HashSet<string> optionNames = new();

        foreach (SpectrumOptionNode option in spectrumDeclaration.Options)
        {
            if (!optionNames.Add(option.Name))
            {
                ErrorFound?.Invoke(Errors.DuplicatedOptionInOutcomeDeclaration(option.Name, spectrumDeclaration.Index));
                error = true;
            }

            if (option.Denominator == 0)
            {
                ErrorFound?.Invoke(Errors.SpectrumBoundDivisionByZero(spectrumDeclaration.Name, option.Name, option.Index));
                error = true;
            }
            else if (option.Numerator > option.Denominator)
            {
                ErrorFound?.Invoke(Errors.SpectrumBoundNotInRange(spectrumDeclaration.Name, option.Name, option.Index));
                error = true;
            }
            else if (option.Numerator < 0 || option.Denominator < 0)
            {
                Debug.Assert(false);
            }
        }

        if (spectrumDeclaration.DefaultOption is not null && !optionNames.Contains(spectrumDeclaration.DefaultOption))
        {
            ErrorFound?.Invoke(Errors.OutcomeDefaultOptionNotAnOption(spectrumDeclaration.Name, spectrumDeclaration.Index));
            error = true;
        }

        if (spectrumDeclaration.Options.Length == 0)
        {
            Debug.Assert(error);
            return (table, null);
        }

        if (error)
        {
            return (table, null);
        }

        static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        static int Lcm(int a, int b)
        {
            return a * b / Gcd(a, b);
        }

        int commonDenominator = 1;

        foreach (SpectrumOptionNode option in spectrumDeclaration.Options)
        {
            commonDenominator = Lcm(commonDenominator, option.Denominator);
        }

        Debug.Assert(commonDenominator != 0);

        ImmutableDictionary<string, SpectrumInterval>.Builder intervalBuilder = ImmutableDictionary.CreateBuilder<string, SpectrumInterval>();

        (int, int) previousFraction = (0, 1);
        int previousNumerator = 0;
        bool previousInclusive = true;

        foreach (SpectrumOptionNode option in spectrumDeclaration.Options)
        {
            // let x be the common denominator
            // then a fraction a/b = (a * (x/b))/x
            // x/b is an integer by definition of lcm
            SpectrumInterval interval = new()
            {
                Inclusive = option.Inclusive,
                UpperDenominator = commonDenominator,
                UpperNumerator = option.Numerator * commonDenominator / option.Denominator,
            };

            if (interval.UpperNumerator <= previousNumerator)
            {
                // this is only okay if the previous one was exclusive and this one is inclusive
                if (previousInclusive || !option.Inclusive)
                {
                    ErrorFound?.Invoke(Errors.SpectrumNotIncreasing(spectrumDeclaration.Name, previousFraction, (option.Numerator, option.Denominator), option.Index));
                    error = true;
                }
            }

            intervalBuilder.Add(option.Name, interval);

            previousFraction = (option.Numerator, option.Denominator);
            previousNumerator = interval.UpperNumerator;
            previousInclusive = interval.Inclusive;
        }

        SpectrumSymbol symbol = new()
        {
            Name = spectrumDeclaration.Name,
            Intervals = intervalBuilder.ToImmutable(),
            OptionNames = spectrumDeclaration.Options.Select(o => o.Name).ToImmutableArray(),
            DefaultOption = spectrumDeclaration.DefaultOption,
            Index = spectrumDeclaration.Index,
        };

        table = table.Declare(symbol);

        return (table, symbol);
    }
}
