# 1.1 Concepts
This document outlines the most important and fundamental concepts of the Historia language.

## 1.1.1 States
A state is a discrete situation in which the story may be at a certain point. There exist two special states: `NotStarted` and `Finished`, all the other states are specified in the Historia script. Let $S$ be the set of all states.

From this state, zero or more different following states may be reached. The states are called the <u>successors</u> of this state. If the state is called $s$, the set of all outgoing states is denoted $(s \rightarrow)$. For two states $s, t \in S$ we write $s \rightarrow t$ (read: $t$ follows $s$) iff $t \in (s \rightarrow)$. We also defined the set of <u>predecessors</u> $(\rightarrow s) = \{ s' \in S \mid s' \rightarrow s \}$.

The `Finished` state is the only state with zero successors, whereas the `NotStartedState` is the only one with no predecessors, i.e. $(\text{Finished} \rightarrow) = \emptyset$ and $(\rightarrow \text{NotStarted}) = \emptyset$.

A state may or may not hold a value, its <u>output</u>. A state is <u>visible</u> iff it has an output. A state is <u>invisible</u> iff it is not visible.

A state $s$ such that $\lvert (s \rightarrow) \rvert \leq 1$ is called <u>linear</u>. A state $s$ such that $\lvert (s \rightarrow) \rvert > 1$ is called <u>non-linear</u>.

## 1.1.2 Flow Graph
The flow graph of a story is a tuple $(S, E, W)$ where $S$ is the set of all states (also called <u>vertices</u>), $E = \{ (s, t) \in S^2 \mid s \rightarrow t \}$ is the set of <u>edges</u> and $W \subseteq E$ is the set of <u>weak edges</u>. The final component of an edge is its index.

The graph $(S, E \setminus W)$ is required to be a directed acyclic graph (DAG), whereas the entire graph $(S, E)$ need not be.

For any state $s$, we call $s^*$ the graph that simply contains this state along with `NotStarted` and `Finished`, and the only edges are $\text{NotStarted} \rightarrow s$ and $s \rightarrow \text{Finished}$.

Let $s$ be a state $G$ be a flow graph. In a different flow graph, the statement $s \rightarrow G$ is to be read as $s$ pointing to the start of $G$ (in other words, replacing its start vertex), and similarly $G \rightarrow s$. Those are statements with a truth value, not a declaration.

Take two flow graphs $G_1, G_2$. We call $G_1 \Rightarrow G_2$ the <u>linear composition</u> of those two graphs. That is the graph where every state in $G_1$ that points to `Finished` instead points to every state in $G_2$ that $G_2$'s `NotStarted` points to, and other than that is the simple union of those two graphs. For a graph $G$ and graphs $H_0, H_1, \dots, H_k$, the graph

$$
G \begin{cases} \Rightarrow H_0 \\ \Rightarrow H_1 \\ \vdots \\ \Rightarrow H_k \end{cases}
$$

is analogously defined, where $G$ points to all of the $H_i$. All $H_i$'s `Finished` states are merged into one.

## 1.1.3 Status
The status of the story at a certain point is the state it is currently in, as well as certain other information that may be required to be stored for a semantically correct story. We denote the set of all status information (minus the current state) as $T$.

## 1.1.4 State Transition Function
The state transition function is a function $f : S \times T \times \mathbb{N} \rightarrow (S \times T) \cup \{ \bot \}$. That is, it maps the status of a story at certain point to the status is has next. The natural number here is the selected option for visible non-linear states to select the next state. $\bot$ represents the fact that something went wrong.

We make the following requirements:
* if there exist $s, s' \in S, t, t' \in T, n \in \mathbb{N}$ such that $f(s, t, n) = (s', t')$, then $s \rightarrow s'$
* if $s \in S$ is linear, for all $n \in \mathbb{N}, t \in T$, if $n \neq 0$, $f(s, t, n) = \bot$, that is, calling $f$ with a linear state and an option $\neq 0$ is invalid
* for all $t \in T, n \in \mathbb{N}$, $f(\text{Finished}, t, n) = \bot$, i.e. there is no transition from the `Finished` state.

## 1.1.5 Paths
An <u>input path</u> is a sequence $n_0, n_1, \dots, n_k$. We also define the <u>state path</u> as a sequence $s_i$ and <u>status path</u> as a sequence $t_i$. The input sequence is valid, iff $f(s_i, t_i, n_i) \neq \bot$, and the other sequences are only defined in that case. Then, $(s_i, t_i) = f(s_{i - 1}, t_{i - 1})$, defining $s_0 = \text{NotStarted}$ and $t_0$ as a sensible start status. Status is more thoroughly defined later, where this will become clearer.

More generally, a <u>path</u> refers to the sequence $(n_i, s_i, t_i)$, where $n_i$ is a valid input path and $s_i$ and $t_i$ are the corresponding state and status paths.
