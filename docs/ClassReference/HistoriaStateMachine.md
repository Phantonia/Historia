# Class `HistoriaStateMachine`
Represents the state machine for your story. Note that it is called `{StoryName}StateMachine`, where `StoryName` is specified by `setting StoryName`. For the rest of this article, it will be called `HistoriaStateMachine`.

`TOutput` is the output type as specified by `setting OutputType`, `TOption` is the option type as specified by `setting OptionType`.

```cs
public sealed class HistoriaStateMachine : Phantonia.Historia.IStoryStateMachine<TOutput, TOption>
```

## Constructors
```cs
public HistoriaStateMachine(IInterface1 reference1, ..., IInterfaceN referenceN);
```

Creates a new state machine from the references as specified in the Historia source code. It is in a state, where `HistoriaStateMachine.NotStartedStory` is true and `HistoriaStateMachine.Output` is the default value. Therefore, in order to use the state machine, `HistoriaStateMachine.TryContinue()` has to be called.

## Properties
### `NotStartedStory`
```cs
public bool NotStartedStory { get; }
```

Specifies whether the state machine is in the beginning state where `HistoriaStateMachine.TryContinue()` has to be called in order to proceed.

### `FinishedStory`
```cs
public bool FinishedStory { get; }
```

Specifies whether the state machine is in the ending state where it cannot continue anymore.

### `CanContinueWithoutOption`
```cs
public bool CanContinueWithoutOption { get; }
```

Is true if and only if `HistoriaStateMachine.TryContinue()` will continue and return `true`.

### `Output`
```cs
public TOutput? Output { get; }
```

Specifies the current output value. When `HistoriaStateMachine.NotStartedStory` is true, it is the default value. Otherwise, it can safely be assumed to not be the default value, in particular, not `null`.

### `Options`
```cs
public Phantonia.Historia.ReadOnlyList<TOption> Options { get; }
```

Specifies the current options. If `HistoriaStateMachine.Options.Count == 0`, `HistoriaStateMachine.TryContinueWithOption(int option)` will always return `false`, else for `0 <= optio < HistoriaStateMachine.Options.Count`, calling `HistoriaStateMachine.TryContinueWithOption(option)` will continue with that option and return `true`.

### `OutcomeXY`
```cs
public OutcomeXY OutcomeXY { get; }
```

Let `XY` be a public outcome as defined in the Historia code. Then `OutcomeXY` will return the current value of this outcome as a value of the enum `OutcomeXY`, which is unset if the outcome has not yet been assigned a value.

### `SpectrumXY`
```cs
public SpectrumXY SpectrumXY { get; }
```

Let `XY` be a public spectrum as defined in the Historia code. Then `SpectrumXY` will return the current option of the spectrum as a value of the enum `SpectrumXY`, which is unset if the spectrum has the value $\frac{0}{0}$.

### `ValueXY`
```cs
public double ValueXY { get; }
```

Let `XY` be a public spectrum as defined in the Historia code. Then `ValueXY` will return a double in the interval $[0, 1]$ which represents the current value of the spectrum, or `double.NaN` if the spectrum has the value $\frac{0}{0}$.

### `ReferenceXY`
```cs
public IInterfaceXY ReferenceXY { get; }
```

Let `XY` be a reference of interface type `InterfaceXY`. Then this property will return that reference.

## Methods
### `TryContinue()`
```cs
public bool TryContinue();
```

Attempts to perform a state transition without specifying an option. It returns `true` if a state transition could be performed, else `false`. Note that it will return `true` if and only if `HistoriaStateMachine.CanContinueWithoutOption` is `true`.

### `TryContinueWithOption`
```cs
public bool TryContinueWithOption(int option);
```

If the current state in the story is a `switch` or `loop switch`, which results in a non-zero amount of `HistoriaStateMachine.Options`, calling `HistoriaStateMachine.TryContinueWithOption` with an option that is an index into `HistoriaStateMachine.Options` performs a state transition into one of the branches of the `switch` or `loop switch`.

`HistoriaStateMachine.TryContinueWithOption(option)` will transition and return `true` if and only if `0 <= option < HistoriaStateMachine.Options.Count`.

### `CreateSnapshot`
```cs
public HistoriaSnapshot CreateSnapshot();
```

Creates a [snapshot](HistoriaSnapshot.md) at the exact state the state machine is currently in.

### `RestoreSnapshot`
```cs
public void RestoreSnapshot(HistoriaSnapshot snapshot);
```

Sets this state machine to the state represented by this [snapshot](HistoriaSnapshot.md).

### `RestoreChapter`
```cs
public void RestoreChapter(HistoriaChapter chapter);
```

Sets the state of this state machine to the beginning of the chapter that this [chapter object](HistoriaChapter.md) represents.
