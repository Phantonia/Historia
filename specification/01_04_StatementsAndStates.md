# 1.4 Statements and States
A scene is composed of multiple statements. With exceptions, each statement corresponds to one state, and the way the statements are set up, the edges are implied.

```cs
Statement : OutputStatement
          | SwitchStatement
          | OutcomeDeclaration
          | SpectrumDeclaration
          | OutcomeAssignmentStatement
          | SpectrumAdjustmentStatement
          | BranchOnStatement
          | CallStatement
          ;
```

## 1.4.1 Flow graph composition rules
Each sequence of statements define a flow graph. For the different kinds of statements, their corresponding sections will specify how they look. First, we specify how these flow graphs compose.

Take the following code:

```
statements1;
statements2;
```

Here, `statements1` and `statements2` are placeholders for arbitrarily many statements. Let $G_1$ and $G_2$ be their flow graphs. The composed flow graph, i.e. the flow graph for the above code, is then simply $G_1 \Rightarrow G_2$.

## 1.4.2 Output Statements
An output statement defines a linear visible state with a given output value. Its syntax is the following:

```cs
OutputStatement : 'output' Expression ';';
```

The expression given here has to be compatible with the output type as specified by the corresponding setting (see [Settings](01_03_TopLevel.md#134-settings) and [Type Compatibility](01_05_TypesAndExpressions.md#153-type-compatibility)), and evaluated it produces the output value.

The output statement $s$ in isolation defines the graph $s^*$.

## 1.4.3 Switch Statements
A switch statement defines a non-linear visible state as well as all of the different states that come after it. Its syntax is the following:

```cs
SwitchStatement : 'switch' identifier? '(' Expression ')' '{' SwitchOption+ '}';
SwitchOption : 'option' identifier? '(' Expression ')' '{' Statement* '}';
```

For each switch statement, if it has a name, all of its options have to have a name, and if it does not have a name, all of the options must not have a name. A switch with a name is referred to as a <u>named switch</u>. For a named switch, all of the option names have be different, but are not bound to any other restrictions (i.e. may repeat the name of the switch or any other name in scope).

The expression at the top of the switch has to be compatible with the output type, and the expressions for all the options have to be compatible with the option type, also as defined by the corresponding [setting](01_03_TopLevel.md#134-settings). See [Type Compatibility](01_05_TypesAndExpressions.md#153-type-compatibility).

Each of the option bodies (the statements enclosed by `{}`) recursively defines its own graph. Let those graphs be $G_0, G_1, \dots, 
G_k$. Also, let $s$ be the switch statement's state. The corresponding graph is then

$$
s^* \begin{cases} \Rightarrow G_0 \\ \Rightarrow G_1 \\ \vdots \\ \Rightarrow G_k \end{cases}
$$

A named switch is syntax sugar for a local outcome. The following two pieces of code are equivalent:

```
// named switch variant
switch Outcome (expression)
{
    option Option0 (expression)
    {
        // ...
    }

    ...

    option OptionK (expression)
    {
        // ...
    }
}

// explicit outcome variant
outcome Outcome (Option0, ..., OptionK);

switch (expression)
{
    option (expression)
    {
        Outcome = Option0;
        // ...
    }

    ...

    option (expression)
    {
        Outcome = OptionK;
        // ...
    }
}
```

More information on outcomes, later.

## 1.4.4 Outcome Declarations
Outcomes and spectrums (from now on generalised as outcomes) can also be declared locally. The syntax and semantics are identical to [Global Outcomes](01_02_Symbols.md#123-outcome-symbols).

Local outcomes are subject to scoping rules. Each body (that is a `'{' Statement* '}'`) introduces a new child scope. If a body $A$ introduces a scope, and inside this body there is a nested body $B$, $A$ is the <u>parent scope</u> of $B$ and $B$ is the <u>child scope</u> of $A$. For any scope defined in the top level, i.e. a scene body, its parent scope is the global scope, where top level symbol declarations declare their symbols. A name exists in a scope if there is a symbol declared in that scope with that name, or the name recursively exists in the parent scope. No declaration is allowed to declare a symbol with a name that already exists in this scope.

Local declarations do not define any state and are simply skipped when building the flow graph.

## 1.4.5 Outcome Assignment
Outcome assignment statements select one of the options for a classic outcome. The result state is linear and invisible.

The syntax is the following:

```cs
OutcomeAssignmentStatement : identifier '=' identifier ';';
```

The first identifier is the name of the outcome to assign to, the second one is the selected option. There has to exist an outcome with that name in scope, and the option has to be an option of the outcome.

Outcome assignments are subject to possible assignment. For more information, see [Possible Assignment]().

## 1.4.6 Spectrum Adjustments
Adjusting a spectrum means performing one of the operations Strengthening and Weakening (see [Spectrums](01_02_Symbols.md#1232-spectrums)). Spectrum adjustments result in linear and invisible states.

The syntax is the following:

```cs
SpectrumAdjustmentStatement : ('strengthen' | 'weaken') identifier 'by' integer_literal ';';
```

The keyword at the beginning chooses which of these operations to perform on the spectrum specified by the identifier. There has to exist a spectrum with that name in scope. The integer literal is the amount, called $n$ in the spectrum specification.

The result flow graph of a single spectrum adjustment $s$ is $s^*$.

## 1.4.7 Branch On Statements
A branch on statement results in a non-linear invisible state. It specifies an outcome and depending on which option that outcome takes on, the branch on state transitions into different following states.

The syntax is the following:

```cs
BranchOnStatement : 'branchon' identifier '{' NamedBranchOnOption* OtherBranchOnOption? '}';
NamedBranchOnOption : 'option' identifier '{' Statement* '}';
OtherBranchOnOption : 'other' '{' Statement* '}';
```

The identifier after the `branchon` keyword references an outcome that has to exist in scope. Each option references one of the options of the outcome. No option may be referenced more than once. An `other` branch may only exist if the remaining options are not exhaustive. If no `other` branch exists, however, the remaining options have to be exhaustive.

Depending on the option that the outcome has taken on, the branch-on state will transition into one of the flow graphs defined by the option bodies. If the outcome option is listed, its corresponding graph is chosen, else the `other` graph.

The outcome has to be definitely assigned at the point of the branch-on statement, as defined by [Definite Assignment]().

Let $G_0, G_1, \dots, G_k$ be the flow graphs for all the option bodies, and let $s$ be the state for the branch-on statement. The result flow graph looks like this:

$$
s^* \begin{cases} \Rightarrow G_0 \\ \Rightarrow G_1 \\ \vdots \\ \Rightarrow G_k \end{cases}
$$

## 1.4.8 Call Statements
Call statements call a different scene $S$, in effect continuing after $S$'s `NotStarted` state. Instead of $S$'s `Finished` state, the story transitions to the call state's next state. For more information on scenes, see [Scenes](01_02_Symbols.md#122-scene-symbols).

The syntax is the following:

```cs
CallStatement : 'call' identifier ';';
```

The identifier has to refer to a scene symbol in scope.

Since scenes are required to be acyclic (that is, no scene may directly or indirectly call itself), all scene graphs can and have to be completely embedded. For more information, see [Scene Embedding]().
