using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Phantonia.Historia.Language;

public readonly record struct InterpretationResult
{
    public InterpretationResult() { }

    public ImmutableArray<Error> Errors { get; init; } = [];

    public InterpreterStateMachine? StateMachine { get; init; }

    [MemberNotNullWhen(returnValue: true, nameof(StateMachine))]
    public bool IsValid
    {
        get
        {
            Debug.Assert(Errors.Length == 0 ^ StateMachine is null); // xor
            return StateMachine is not null;
        }
    }
}
