﻿using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language;

public static class Errors
{
    public static Error BrokenStringLiteral(Token brokenToken)
    {
        Debug.Assert(brokenToken is { Kind: TokenKind.BrokenStringLiteral });

        return new Error
        {
            ErrorMessage = $"Broken string literaral {brokenToken.Text}",
            Index = brokenToken.Index,
        };
    }

    public static Error ExpectedToken(Token unexpectedToken, TokenKind expectedKind)
    {
        Debug.Assert(unexpectedToken.Kind != expectedKind);

        return new Error
        {
            ErrorMessage = $"Expected a {expectedKind} token, instead got '{unexpectedToken.Text}'",
            Index = unexpectedToken.Index,
        };
    }

    public static Error UnexpectedToken(Token unexpectedToken)
    {
        return new Error
        {
            ErrorMessage = $"Unexpected token '{unexpectedToken.Text}'",
            Index = unexpectedToken.Index,
        };
    }

    public static Error SettingDoesNotExist(Token identifierToken)
    {
        Debug.Assert(identifierToken is { Kind: TokenKind.Identifier });

        return new Error
        {
            ErrorMessage = $"Setting '{identifierToken.Text}' does not exist",
            Index = identifierToken.Index,
        };
    }

    public static Error UnexpectedEndOfFile(Token endOfFileToken)
    {
        Debug.Assert(endOfFileToken is { Kind: TokenKind.EndOfFile });

        return new Error
        {
            ErrorMessage = "Unexpected end of file",
            Index = endOfFileToken.Index,
        };
    }

    public static Error SymbolDoesNotExistInScope(string identifier, int index)
    {
        return new Error
        {
            ErrorMessage = $"Symbol '{identifier}' does not exist in this scope",
            Index = index,
        };
    }

    public static Error NonTypeSymbolUsedAsType(string identifier, int index)
    {
        return new Error
        {
            ErrorMessage = $"Symbol '{identifier}' is not a type but is used like one",
            Index = index,
        };
    }

    public static Error IncompatibleType(TypeSymbol sourceType, TypeSymbol targetType, string context, int index)
    {
        return new Error
        {
            ErrorMessage = $"Type '{sourceType.Name}' is not compatible with {context} type '{targetType.Name}'",
            Index = index,
        };
    }

    public static Error RecordDoesNotExist(string recordName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Record named '{recordName}' does not exist",
            Index = index,
        };
    }

    public static Error SymbolIsNotRecord(string symbolName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Symbol named '{symbolName}' is not a record",
            Index = index
        };
    }

    public static Error WrongAmountOfArguments(string recordName, int givenAmount, int expectedAmount, int index)
    {
        return new Error
        {
            ErrorMessage = $"Record '{recordName}' has {expectedAmount} propert{(expectedAmount != 1 ? "ies" : "y")}, but {givenAmount} argument{(expectedAmount != 1 ? "s were" : " was")} provided",
            Index = index,
        };
    }

    public static Error WrongPropertyInRecordCreation(string propertyName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Property '{propertyName}' either does not exist or is not in that position",
            Index = index,
        };
    }

    public static Error SymbolIsNotOutcome(string symbolName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Symbol named '{symbolName}' is not an outcome or a named switch",
            Index = index
        };
    }

    public static Error OptionDoesNotExistInOutcome(string outcomeName, string optionName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The outcome or named switch '{outcomeName}' does not have an option named '{optionName}'",
            Index = index,
        };
    }

    public static Error BranchOnDuplicateOption(string outcomeName, string optionName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The branchon statement for outcome or named switch '{outcomeName}' " +
                           $"has more than one branch for the option named '{optionName}'",
            Index = index,
        };
    }

    public static Error BranchOnIsNotExhaustive(string outcomeName, IEnumerable<string> missingOptionNames, int index)
    {
        missingOptionNames = missingOptionNames.Select(n => $"'{n}'");

        return new Error
        {
            ErrorMessage = $"The branchon statement does not cover all options of the outcome or named switch '{outcomeName}' " +
                           $"(it is missing the options {string.Join(", ", missingOptionNames)}). " +
                           $"Add an empty other branch if this is intentional, else this is probably an error",
            Index = index,
        };
    }

    public static Error BranchOnIsExhaustiveAndHasOtherBranch(string outcomeName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The branchon statement covers every option of the outcome or named switch '{outcomeName}' " +
                           $"but still has an other branch. This branch will never run. You can safely remove it or comment it out " +
                           $"as you will get an error in case the outcome gets more options in the future anyway",
            Index = index,
        };
    }

    public static Error NoMainScene()
    {
        return new Error
        {
            ErrorMessage = "A story needs a main scene",
            Index = 0,
        };
    }

    public static Error DuplicatedSymbolName(string name, int index)
    {
        return new Error
        {
            ErrorMessage = $"Duplicated symbol name '{name}'",
            Index = index,
        };
    }

    public static Error CyclicTypeDefinition(IEnumerable<string> cycle, int index)
    {
        return new Error
        {
            ErrorMessage = $"Cyclic type definition: {string.Join(", ", cycle.Select(s => s))}",
            Index = index,
        };
    }

    public static Error InconsistentNamedSwitch(int index)
    {
        return new Error
        {
            ErrorMessage = "Every option of a named switch must also be named",
            Index = index,
        };
    }

    public static Error InconsistentUnnamedSwitch(int index)
    {
        return new Error
        {
            ErrorMessage = "No option of an unnamed switch may be named",
            Index = index,
        };
    }

    public static Error DuplicatedOptionInOutcomeDeclaration(string optionName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Option name '{optionName}' appears more than once in outcome declaration or named switch",
            Index = index,
        };
    }

    public static Error OutcomeWithZeroOptions(string outcomeName, int index)
    {
        return new Error
        {
            ErrorMessage = $"An outcome or named switch needs at least one option, outcome '{outcomeName}' has none",
            Index = index,
        };
    }

    public static Error OutcomeDefaultOptionNotAnOption(string outcomeName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The default option of outcome '{outcomeName}' is not one of its options. Add it to the list",
            Index = index,
        };
    }

    public static Error OutcomeAssignedNonIdentifier(string outcomeName, int index)
    {
        return new Error
        {
            ErrorMessage = $"'{outcomeName}' is an outcome, but it isn't assigned one of it's options",
            Index = index,
        };
    }

    public static Error BranchOnOnlyOneOtherLast(int index)
    {
        return new Error
        {
            ErrorMessage = "A branchon statement may only have a single other clause and it has to be after every named option",
            Index = index,
        };
    }

    public static Error SymbolHasNoValue(string name, int index)
    {
        return new Error
        {
            ErrorMessage = $"The symbol '{name}' cannot be used as an expression",
            Index = index,
        };
    }

    public static Error SymbolCannotBeAssignedTo(string name, int index)
    {
        return new Error
        {
            ErrorMessage = $"The symbol '{name}' cannot be assigned to",
            Index = index,
        };
    }

    public static Error OutcomeMayBeAssignedMoreThanOnce(string outcomeName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The outcome '{outcomeName}' may already be assigned once this assignment executes. Keep in mind that outcomes may only be assigned once",
            Index = index,
        };
    }

    public static Error OutcomeNotDefinitelyAssigned(string outcomeName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The outcome '{outcomeName}' may not be assigned once this statement executes",
            Index = index,
        };
    }

    public static string GenerateFullMessage(string text, Error error)
    {
        (string wholeLine, int column) = FindLine(text, error.Index);

        return $"Error: {error.ErrorMessage}{Environment.NewLine}{wholeLine}{Environment.NewLine}{new string(' ', column)}^";
    }

    private static (string wholeLine, int column) FindLine(string text, int index)
    {
        int lineStartIndex = 0;

        for (int i = index - 1; i >= 0; i--)
        {
            if (text[i] == '\n')
            {
                lineStartIndex = i + 1;
                break;
            }
        }

        // column is 0-based, so it is also the number of characters before it in the line
        int column = index - lineStartIndex;

        int lineEndIndex = text.Length;

        for (int i = index; i < text.Length; i++)
        {
            if (text[i] is '\n' or '\r')
            {
                lineEndIndex = i;
                break;
            }
        }

        return (text[lineStartIndex..lineEndIndex], column);
    }
}
