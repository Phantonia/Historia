using Phantonia.Historia.Language.LexicalAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language;

public static class Errors
{
    public static Error UnterminatedStringLiteral(int index)
    {
        return new Error
        {
            ErrorMessage = $"String literal is not terminated before line break or end of file",
            Index = index,
        };
    }

    public static Error InvalidEscapeSequence(int index)
    {
        return new Error
        {
            ErrorMessage = $"String literal contains invalid escape sequence",
            Index = index,
        };
    }

    public static Error InvalidCharacter(int index)
    {
        return new Error
        {
            ErrorMessage = $"String literal contains in invalid character (use \\uXXXX) notation instead",
            Index = index,
        };
    }

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

    public static Error ExpectedVisibleStatementAsCheckpoint(Token unexpectedToken)
    {
        return new Error
        {
            ErrorMessage = $"The 'checkpoint' keyword is only valid for visible statements, so expected one of the following keywords: " +
                $"'output', 'switch', 'loop switch', instead got '{unexpectedToken.Text}'",
            Index = unexpectedToken.Index,
        };
    }

    public static Error MustHaveAtLeastOneOption(int index)
    {
        return new Error
        {
            ErrorMessage = "A switch or choose statement must have at least one option",
            Index = index,
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

    public static Error SymbolIsNotEnum(string symbolName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Symbol named '{symbolName}' is not an enum",
            Index = index
        };
    }

    public static Error WrongAmountOfArgumentsInRecordCreation(string recordName, int givenAmount, int expectedAmount, int index)
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
            ErrorMessage = $"Symbol named '{symbolName}' is not an outcome",
            Index = index
        };
    }

    public static Error OptionDoesNotExistInOutcome(string outcomeName, string optionName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The outcome '{outcomeName}' does not have an option named '{optionName}'",
            Index = index,
        };
    }

    public static Error BranchOnDuplicateOption(string outcomeName, string optionName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The branchon statement for outcome '{outcomeName}' " +
                           $"has more than one branch for the option named '{optionName}'",
            Index = index,
        };
    }

    public static Error BranchOnIsNotExhaustive(string outcomeName, IEnumerable<string> missingOptionNames, int index)
    {
        missingOptionNames = missingOptionNames.Select(n => $"'{n}'");

        return new Error
        {
            ErrorMessage = $"The branchon statement does not cover all options of the outcome '{outcomeName}' " +
                           $"(it is missing the options {string.Join(", ", missingOptionNames)}). " +
                           $"Add an empty other branch if this is intentional, else this is probably an error",
            Index = index,
        };
    }

    public static Error BranchOnIsExhaustiveAndHasOtherBranch(string outcomeName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The branchon statement covers every option of the outcome '{outcomeName}' " +
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
            ErrorMessage = $"Cyclic type definition: {string.Join(", ", cycle.Select(s => $"'{s}'"))}",
            Index = index,
        };
    }

    public static Error CyclicSceneDefinition(IEnumerable<string> cycle, int index)
    {
        return new Error
        {
            ErrorMessage = $"The scene '{cycle.First()}' directly or indirectly calls itself; cycle: {string.Join(", ", cycle.Select(s => $"'{s}'"))}",
            Index = index,
        };
    }

    public static Error LoopSwitchHasToTerminate(int index)
    {
        return new Error
        {
            ErrorMessage = "A looped switch has to be able to terminate, in other words: If there is at least one looped option, there also has to be at least one final option",
            Index = index,
        };
    }

    public static Error DuplicatedOptionInOutcomeDeclaration(string optionName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Option name '{optionName}' appears more than once in outcome declaration",
            Index = index,
        };
    }

    public static Error OutcomeWithZeroOptions(string outcomeName, int index)
    {
        return new Error
        {
            ErrorMessage = $"An outcome needs at least one option, outcome '{outcomeName}' has none",
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
            ErrorMessage = $"'{outcomeName}' is an outcome, but it isn't assigned one of its options",
            Index = index,
        };
    }

    public static Error SpectrumNotIncreasing(string spectrumName, (int num, int denom) lastOkay, (int num, int denom) offending, int index)
    {
        return new Error
        {
            ErrorMessage = $"The options in the spectrum '{spectrumName}' are not increasing: {lastOkay.num}/{lastOkay.denom} >= {offending.num}/{offending.denom}",
            Index = index,
        };
    }

    public static Error SpectrumBoundDivisionByZero(string spectrumName, string offendingOption, int index)
    {
        return new Error
        {
            ErrorMessage = $"In the spectrum '{spectrumName}' the option '{offendingOption}' divides by zero",
            Index = index,
        };
    }

    public static Error SpectrumBoundNotInRange(string spectrumName, string offendingOption, int index)
    {
        return new Error
        {
            ErrorMessage = $"The option '{offendingOption}' of the spectrum '{spectrumName}' has a bound greater than 1 or less than 0",
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

    public static Error SymbolIsNotSpectrum(string name, int index)
    {
        return new Error
        {
            ErrorMessage = $"Symbol '{name}' is not a spectrum, so it cannot be strengthened/weakened",
            Index = index,
        };
    }

    public static Error OutcomeMightBeAssignedMoreThanOnce(string outcomeName, IEnumerable<string> callStack, int index)
    {
        return new Error
        {
            ErrorMessage = $"The outcome '{outcomeName}' might already be assigned once this assignment executes through the following scene calls: {string.Join(", ", callStack.Reverse())}. Keep in mind that outcomes may only be assigned once in a given walkthrough",
            Index = index,
        };
    }

    public static Error OutcomeNotDefinitelyAssigned(string outcomeName, IEnumerable<string> callStack, int index)
    {
        return new Error
        {
            ErrorMessage = $"The outcome '{outcomeName}' might not be assigned once this statement executes through the following scene calls: {string.Join(", ", callStack.Reverse())}. Consider adding a default option",
            Index = index,
        };
    }

    public static Error SpectrumNotDefinitelyAssigned(string spectrumName, IEnumerable<string> callStack, int index)
    {
        return new Error
        {
            ErrorMessage = $"The spectrum '{spectrumName}' might never be strengthened/weakened, so has an undetermined value, when this statement executes through the following scene calls: {string.Join(", ", callStack.Reverse())}. Consider adding a default option",
            Index = index,
        };
    }

    public static Error OutcomeIsLocked(string outcomeName, IEnumerable<string> callStack, int index)
    {
        return new Error
        {
            ErrorMessage = $"The outcome '{outcomeName}' cannot be used anymore because it is locked through the following scene calls: {string.Join(", ", callStack.Reverse())}. " +
                $"Outcomes get locked when they are not public and their value might pass a checkpoint threshold. " +
                $"To fix this problem, consider turning this outcome into a public outcome",
            Index = index,
        };
    }

    public static Error SpectrumIsLocked(string spectrumName, IEnumerable<string> callStack, int index)
    {
        return new Error
        {
            ErrorMessage = $"The spectrum '{spectrumName}' cannot be used anymore because it is locked through the following scene calls: {string.Join(", ", callStack.Reverse())}. " +
                $"Spectrums get locked when they are not public and their value might pass a checkpoint threshold. " +
                $"To fix this problem, consider turning this spectrum into a public spectrum",
            Index = index,
        };
    }

    public static Error UnionHasDuplicateSubtype(string unionName, string typeName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The union '{unionName}' has the subtype '{typeName}' more than once",
            Index = index,
        };
    }

    public static Error InvalidNamespaceFormat(string namespaceString, int index)
    {
        return new Error
        {
            ErrorMessage = $"The string '{namespaceString}' is not a valid dotnet namespace (i.e. identifiers seperated by dots)",
            Index = index,
        };
    }

    public static Error ForbiddenNamespace(string namespaceString, int index)
    {
        return new Error
        {
            ErrorMessage = $"The namespace '{namespaceString}' is either a System, Microsoft or Phantonia namespace. Those are forbidden due to possible conflicts",
            Index = index,
        };
    }

    public static Error InvalidStoryName(string storyName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Story name '{storyName}' is not a valid identifier",
            Index = index,
        };
    }

    public static Error ConflictingStoryName(string storyName, int index)
    {
        // TODO: change this to reflect the StateMachine / Snapshot suffixes
        return new Error
        {
            ErrorMessage = $"Story name '{storyName}' conflicts with a symbol in your story which will also be generated as a type",
            Index = index,
        };
    }

    public static Error ExpectedTrueFalseString(string settingName, string actualString, int index)
    {
        return new Error
        {
            ErrorMessage = $"The setting '{settingName}' requires either the string 'true' or 'false', instead got: '{actualString}'",
            Index = index,
        };
    }

    public static Error ConflictingUnionSubtype(string unionName, string subtypeName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Union '{unionName}' contains a subtype called '{subtypeName}'. That name is reserved for a method/property on the generated union type",
            Index = index,
        };
    }

    public static Error DuplicatedRecordPropertyName(string recordName, string propertyName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Property name '{propertyName}' exists more than once in record '{recordName}'",
            Index = index,
        };
    }

    public static Error ConflictingRecordProperty(string recordName, string propertyName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Record '{recordName}' contains a property called '{propertyName}'. That name is reserved for a method/property on the generated record type",
            Index = index,
        };
    }

    public static Error DuplicatedOptionInEnum(string enumName, string optionName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Option '{optionName}' in enum '{enumName}' is duplicated",
            Index = index,
        };
    }

    public static Error OptionDoesNotExistInEnum(string enumName, string optionName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Enum '{enumName}' does not have an option '{optionName}'",
            Index = index,
        };
    }

    public static Error DuplicatedMethodNameInInterface(string interfaceName, string methodName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Method '{methodName}' in interface '{interfaceName}' is duplicated",
            Index = index,
        };
    }

    public static Error SymbolIsNotInterface(string alledgedInterfaceName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Symbol '{alledgedInterfaceName}' is not an interface",
            Index = index,
        };
    }

    public static Error SymbolIsNotReference(string alledgedReferenceName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Symbol '{alledgedReferenceName}' is not a reference",
            Index = index,
        };
    }

    public static Error MethodDoesNotExistInInterface(string referenceName, string interfaceName, string alledgedMethodName, int index)
    {
        return new Error
        {
            ErrorMessage = $"The interface of reference '{referenceName}', '{interfaceName}', does not contain method '{alledgedMethodName}'",
            Index = index,
        };
    }

    public static Error CannotRunChoiceMethod(string interfaceName, string methodName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Cannot run choice method '{methodName}' of interface '{interfaceName}'",
            Index = index,
        };
    }

    public static Error CannotChooseFromActionMethod(string interfaceName, string methodName, int index)
    {
        return new Error
        {
            ErrorMessage = $"Cannot choose from action method '{methodName}' of interface '{interfaceName}'",
            Index = index,
        };
    }

    public static Error WrongAmountOfArgumentsInMethodCall(string interfaceName, string methodName, int givenAmount, int expectedAmount, int index)
    {
        return new Error
        {
            ErrorMessage = $"Method '{methodName}' of interface '{interfaceName}' has {expectedAmount} parameter{(expectedAmount != 1 ? "s" : "")}, but {givenAmount} argument{(givenAmount != 1 ? "s were" : " was")} provided",
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
