# Reachability
Reachability analysis is a step of the Historia compilation process that asks and answers questions along the lines of "Is this state reachable with an invalid configuration?" In this document, we take a look at the exact problems at hand and the algorithm to answer these questions (which is one of my all time favorite algorithms).

## Definite assignment
The Historia language requires that an outcome has to be assigned before it is used to branch on. Take this code:

```
scene main
{
    outcome X (A, B);

    branchon X
    {
        option A
        {
            output 1;
        }

        option B
        {
            output 2;
        }
    }
}
```

This code is very clearly wrong. When the `branchon` statement runs, the outcome doesn't have a value, so we have no idea which option to run.

However, it gets more subtle:

```
scene main
{
    outcome X (A, B);

    switch (0)
    {
        option (1)
        {
            X = A;
        }

        option (2)
        {
            // we don't assign X here
        }
    }

    branchon X
    {
        option A
        {
            output 1;
        }

        option B
        {
            output 2;
        }
    }
}
```

This code is also wrong: At the `branchon` the outcome might or might not have value, depending on the branch taken at the switch. However, we require that it *always* has a value, no matter what.

In general, the compiler is only happy if it can *prove* that an outcome is definitely assigned. That means, you get an error if the compiler can't prove that, even though in practice it might always be assigned. Take this rather convoluted code:

```
scene main
{
    outcome X (A, B);
    outcome Y (A, B);

    X = A;
    
    branchon X
    {
        option A // we know that this case always runs as X is always A
        {
            Y = A;
        }

        option B
        {
            // we don't assign Y here
        }
    }

    branchon Y // error: Y is not definitely assigned
    {
        option A { }
        option B { }
    }
}
```

The compiler is not clever enough to realise that it is impossible for Y to be unassigned. For all it cares, X could have had the option B, in which case Y is not assigned, so it might not have a value.

In general, if the compiler can't prove definite assignment, it produces an error. This is because it is impossible to always prove or disprove this because of the Halting Problem [note: Historia is not turing complete so it would be generally possible but I don't know of an efficient algorithm, and it potentially becomes more complicated in the future, in which case the Halting Problem may apply].

## Definite Unassignment
This may seem entirely counterintuitive. Previously we required an outcome to be definitely assigned if it is read (i.e. branched on). Now we require that is definitely *unassigned* if it is assigned. That is because once an outcome is assigned one of its options, that choice is final and cannot be changed anymore. What is counterintuitive is that this is not the opposite of definite assignment. A variable can be not definitely assigned and not definitely unassigned at the same time. But let's not get ahead of ourselves. Let's start with a very obvious example:

```
scene main
{
    outcome X (A, B);

    X = A;
    X = B; // error: X might already be assigned
}
```

But this gets more subtle as well:

```
scene main
{
    outcome X (A, B);

    switch (0)
    {
        option (1)
        {
            X = A;
        }

        option (2) { }
    }

    X = B; // error: X might already be assigned
}
```

In this case, X may or may not be assigned twice, we don't know. But we require that is always assigned at most once, which is not the case here, so the compiler produces an error.

As with definite assignment, definite unassignment has the motto "We are only happy if we can prove everything is good, if we can't prove that we error".

Let's get back to why definite unassignment is not the opposite of definite assignment. Definite assignment means: This outcome is always assigned. Definite unassignment means: This outcome is never assigned. But what happens if the outcome is sometimes assigned or not. Specifically, an outcome can be *not definitely assigned* and *not definitely unassigned* at the same time:

```
scene main
{
    outcome X (A, B);

    switch (0)
    {
        option (1)
        {
            X = A;
        }

        option (2) { }
    }

    // at this point X is both *not definitely assigned* and *not definitely unassigned*
    // there are paths where it is assigned and paths where it is unassigned

    switch (10)
    {
        option (11)
        {
            X = B; // error: X may be assigned more than once
        }

        option (12) { }
    }

    branchon X // error: X may not be assigned at all
    {
        option A { }
        option B { }
    }
}
```

## Default Options
An outcome may have a default value specified:

```
scene main
{
    outcome X (A, B) default A;
}
```

This very much affects this whole process. The idea is that if X is not assigned, it has the value A. However, it is not assigned the value A, so it may be assigned something different.

In other words, X is always definitely assigned, but is definitely unassigned. So the following code is perfectly fine:

```
scene main
{
    outcome X (A, B) default A;

    switch (0)
    {
        option (1)
        {
            X = B; // okay, we assign for the first time
        }

        option (2)
        {
            // do nothing
        }
    }

    branchon X // okay, X may not be assigned, but in that case it has the default value A
    {
        option A { }
        option B { }
    }
}
```

## The Inductive Algorithm
How do we perform this analysis? I promised this is one of my favorite algorithms of all time because it is actually very clean.

The compiler generates a so called FlowGraph for this code. Each statement (except for the outcome declaration) is a vertex in this graph, and an edge between two vertices A -> B means that after A runs, B may run next (but A may have edges to more than one vertex).

The FlowGraph is acyclic. That means we can topologically sort it. That means we get an order of vertices where A comes before B in that order iff A directly or indirectly points to B. In other words: If we process the vertices in that order, once we process a vertex V, we know that each vertex before it has already been processed.

Induction can be used to define stuff, prove stuff and also compute stuff. What they all have in common is that they have two parts: The inductive start and the inductive step.

We can inductively define the Fibonacci numbers. The inductive start is that $F_0 = F_1 = 1$. The inductive step is for some $n \in \mathbb{N}, n \geq 2$, $F_n = F_{n - 1} + F_{n - 2}$. Notice how the inductive step can use everything that came before, because the natural numbers have a clear order.

To compute $F_n$ we can use an inductive algorith (in C#):

```cs
void Fib(int n)
{
    int[] numbers = new int[n + 1];
    
    // inductive start
    numbers[0] = 1;
    numbers[1] = 1;

    // inductive step
    for (int i = 2; i <= n; i++)
    {
        numbers[i] = numbers[i - 1] + numbers[i - 2];
    }

    return numbers[n];
}
```

The inductive algorithm for reachability is significantly more complicated, but the principle is the same.

Recall that we have a topological ordering of all vertices. To compute the status of outcomes for a vertex, we can use the status of outcomes for all vertices that point to it, if we compute those first. That's induction.

### Vertex data
For each vertex, we save for each outcome the following two values:

- `bool DefinitelyAssigned`
- `bool MightBeAssigned`

Notice how the first makes a definite statement, whereas the second one is the opposite of a definite statement. This is confusing, but the opposite is also confusing. This way, both somehow resemble the fact something is assigned.

### Inductive start
In the beginning, each outcome is not definitely assigned and definitely unassigned. So `DefinitelyAssigned = false` and `MightBeAssigned = false`. The only exception is if the start vertex assigns an outcome, in which case it is definitely assigned and might also be assigned, so both are `true`.

### Inductive step
For each outcome, it is definitely assigned, if it is definitely assigned in each of the vertices that point to this vertex (pseudo code):

```cs
DefinitelyAssigned = PointingVertices.All(v => v.DefinitelyAssigned);
```

The outcome might be assigned, if any of the pointing vertices assign it:

```cs
MightBeAssigned = PointingVertices.Any(v => v.MightBeAssigned);
```

If the vertex then assigns the outcome, we check `MightBeAssigned`. If that is true, we error. Either way, the outcome is definitely assigned and might be assigned.

If the vertex is a `branchon` statement instead, we error if `DefinitelyAssigned` is false.

### Summary
And that is it. We process each vertex exactly once, and for each vertex we process each outcome exactly once. For each vertex (except for the very first one) we use our results from previous vertices.

The algorithm is purely used to verify the code, it does not produce anything. That is unlike the parser or the binder. The parser builds the AST, and during that process it might notice errors. The binder binds and type checks everything, and in that process it might notice errors. This step of the compiler does not produce a result.
