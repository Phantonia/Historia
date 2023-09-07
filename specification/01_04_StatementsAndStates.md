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

The expression given here has to be compatible with the output type as specified by the corresponding setting (see [Settings](01_03_TopLevel.md#134-settings)), and evaluated it produces the output value.

The output statement $s$ in isolation defines the graph $s^*$.

## 1.4.3 Switch Statements
A switch statement defines a non-linear visible state as well as all of the different states that come after it. Its syntax is the following:

```cs
SwitchStatement : 'switch' identifier? '(' Expression ')' '{' SwitchOption+ '}';
SwitchOption : 'option' identifier? '(' Expression ')' '{' Statement* '}';
```

For each switch statement, if it has a name, all of its options have to have a name, and if it does not have a name, all of the options must not have a name. A switch with a name is referred to as a <u>named switch</u>. For a named switch, all of the option names have be different, but are not bound to any other restrictions (i.e. may repeat the name of the switch or any other name in scope).

The expression at the top of the switch has to be compatible with the output type, and the expressions for all the options have to be compatible with the option type, also as defined by the corresponding [setting](01_03_TopLevel.md#134-settings).

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
