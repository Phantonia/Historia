# 1.2 Symbols
A symbol is an object which can be referred to by name. There are several different kinds of symbols in the Historia language:

## 1.2.1 Type Symbols
A type is a set of values, and an expression produces a value that is an element of its type.

### 1.2.1.1 Builtin Types
The Historia language predefines the following type symbols:
* `Int` is the integer type, and it corresponds to a 32-bit signed integer. It is exactly the `int` type from C#.
* `String` is the string type, and it corresponds to strings of UTF-16 characters. It is exactly the `string` type from C#.

### 1.2.1.2 Records
Records are types that bundle properties that each has its own type. The properties of a record are ordered, named and may have any type. All the property names must be different.

Due to technical reasons, no property of a record may have any of the following names:

* The name of the record itself
* `Equals`
* `GetHashCode`
* `GetType`
* `MemberwiseClone`
* `ReferenceEquals`
* `ToString`
* `op_Equality`
* `op_Inequality`

### 1.2.1.3 Enums
Enums (short for enumerations) are types that are a set of any number (including zero) of named discrete options. Each option must have a different name.

### 1.2.1.4 Unions
Unions are the set union of any number of types. Let $U$ be the union of types $A, B$, that is $U = A \cup B$ [note: this need not be the set theoretical notion of a union]. $A$ and $B$ are called the <u>subtypes</u> of $U$. Unions <u>flatten</u> their subtypes. If $B$ in the previous example is the union of $C$ and $D$, then $U = A \cup B = A \cup (C \cup D) = A \cup B \cup D$, in other words, $U$ is the union of $A, B, D$.

Due to technical reasons, no subtype of a union may have any of the following names:

* The name of the union itself
* `Discriminator`
* `Run`
* `Evaluate`
* `AsObject`
* `Equals`
* `GetHashCode`
* `GetType`
* `MemberwiseClone`
* `ReferenceEquals`
* `ToString`
* `op_Equality`
* `op_Inequality`
* The union name + `Discriminator`

### 1.2.1.5 Type references
A type $A$ <u>directly depends on</u> another type $B$, iff $A$ is a record and $B$ is the type of any of its properties, or $A$ is a union and $B$ is a subtype of this union. A type $A$ is <u>indirectly depends on</u> another type $C$, iff there exist types $B_0, B_1, \dots, B_n$ such that $A$ directly depends on $B_0$, $B_0$ directly depends on $B_1$, ..., and $B_n$ directly depends $C$.

No type may ever directly or indirectly depend on itself.

## 1.2.2 Scene Symbols
A scene specifies a flow graph with the possibility to have states that call other scenes. A scene $A$ <u>directly depends on</u> another scene $B$ iff there exist a call to $B$ in $A$. Analogously to type references, a scene $A$ <u>indirectly depends on</u> another scene $C$, iff there exist scenes $B_0, B_1, \dots, B_n$ such that $A$ directly depends on $B_0$, $B_0$ directly depends on $B_1$, ..., $B_n$ directly depends on $C$.

No scene may ever directly or indirectly depend on itself.

## 1.2.3 Outcome Symbols
Outcomes are ways to save and later reference previously made choices.

### 1.2.3.1 Classic Outcomes
A classic outcome is an outcome which may take on one of finitely many (including zero) named options. All these options have be different. An outcome may optionally have a default value.

<u>Assignment</u> refers to selecting an option for this outcome.

Based on the option assigned to this outcome, the story may later branch. If the story tries to branch on an outcome, there most not exist any input path which reaches this state, where the outcome has never been assigned, unless that outcome has a default value. This rule is called <u>Definite Assignment</u>. A program where such a path exists, is invalid. More programs may be invalid though, given the algorithm for determining this. In other words: A story is invalid, iff it cannot be proven correct, which is not the same thing as saying it is correct. More details about this in a later section of the specification.

Analogously to Definite Assignment, <u>Possible Assignment</u> bans assigning to an outcome which has already been assigned to (the default value does not count). In other words, there may not be an input path, such that an outcome is assigned multiple times.

### 1.2.3.2 Spectrums
A spectrum is a special kind of outcome that continuously changes according to specific operations and rules.

Generally, a spectrum also defines named options where all of these names have to be different. These options are assigned intervals $I \subseteq [0, 1]$ [where the numbers are restricted to rational numbers with 32-bit integers as numerators and denominators]. These intervals partition $[0, 1]$, i.e. are all pairwise disjoint, non-empty and their union is exactly the interval $[0, 1]$. A spectrum may or may not have a default option, which in that case has to be one of the listed options.

At first, the spectrum is undefined, as it is the ratio $\frac{0}{0}$. There exist two operations: <u>strengthen</u> and <u>weaken</u>. When the spectrum is strengthened, the ratio $\frac{p}{t}$ [where $p$ stands for positive and $t$ stands for total] is transformed into $\frac{p + n}{t + n}$ for some positive given number $n$. This increases the ratio, as $\frac{p}{t} \leq \frac{p + n}{t + n} \leq 1$. In fact, if $p < t$, then $\frac{p}{t} < \frac{p + n}{t + n} < 1$. Conversely, weakening the spectrum means only increasing the total, i.e. transforming $\frac{p}{t}$ into $\frac{p}{t + n}$. This decreases the ratio, as $\frac{p}{t} > \frac{p}{t + n} > 0$, if $p < t$.

Let $I_0, I_1, \dots, I_k$ be the defined intervals for the options. Since these intervals partition $[0, 1]$, for any spectrum ratio $\frac{p}{t}$, there exists a unique interval $I_i$, such that $\frac{p}{t} \in I_i$. When the spectrum is branched on, each option, so each interval, is associated with a next state, and depending on which interval the ratio belongs to, the next state is chosen.

<u>Definite Assignment</u> also exists for spectrums, requiring a spectrum to not be undefined when branched on, unless it has a default option, which is chosen in case of an undefined ratio.
