# 1.3 Top Level
A Historia script (called a story) is composed of several top level nodes.

```cs
Story : TopLevelNode*;
TopLevelNode : SettingDirective
             | SceneDeclaration
             | RecordDeclaration
             | EnumDeclaration
             | UnionDeclaration
             | OutcomeDeclaration
             | SpectrumDeclaration
             ;
```

## 1.3.1 Type Declarations
### 1.3.1.1 Record Declarations
For more information on records, see [Records](01_02_Symbols.md#1212-records).

The syntax for record declarations is the following:

```cs
RecordDeclaration : 'record' identifier '(' (PropertyDeclaration (',' PropertyDeclaration)* ','?)? ')' ';';
PropertyDeclaration : identifier ':' Type;
```

Note the possible trailing comma in the list of properties.

The identifier at the beginning, after the `record` keyword is the name of the record, and for each property declaration, its identifier is the name of the property. All the property names have be different.

### 1.3.1.2 Enum Declarations
For more information on enums, see [Enums](01_02_Symbols.md#1213-enums).

The syntax for enum declarations is the following:

```cs
EnumDeclaration : 'enum' identifier '(' (identifier (',' identifier)* ','?)? ')' ';';
```

The identifier at the beginning, after the `enum` keyword is the name of the enum type, and the other identifiers are the names of the options of the enum. All the option names have be different.

Note the possible trailing comma in the list of options.

### 1.3.1.3 Union Declarations
For more information on unions, see [Unions](01_02_Symbols.md#1214-unions).

The syntax for union declarations is the following:

```cs
UnionDeclaration : 'union' identifier '(' Type (',' Type)* ','? ')' ';';
```

The identifier at the beginning, after the `union` keyword is the name of the union type, and the types in parentheses are the subtypes of this union declaration. There is no restriction requiring all subtypes to be different, because by flattening, a subtype may appear more than once anyway.

Note the possible trailing comma in the list of subtypes.

## 1.3.2 Scene Declarations
Fore more information on scenes, see [Scenes](01_02_Symbols.md#122-scene-symbols).

The syntax for scene declarations is the following:

```cs
SceneDeclaration : 'scene' identifier '{' Statement* '}'
```

The identifier is the name of the scene, the sequence of statements in `{}` is called the <u>scene body</u>.

A story is never valid, if it doesn't contain a scene declaration with the name `main`. This is the <u>main scene</u>.

For statements, see [Statements and States](01_04_StatementsAndStates.md).

## 1.3.3 Outcome Declarations

### 1.3.3.1 Classic Outcomes
For more information on classic outcomes, see [Classic Outcomes](01_02_Symbols.md#1231-classic-outcomes).

The syntax for classic outcome declarations is the following:

```cs
OutcomeDeclaration : 'outcome' identifier '(' identifier (',' identifier)* ','? ')' ('default' identifier)? ';';
```

The identifier at the beginning, after the `outcome` keyword is the name of the outcome, and the other identifiers are the names of the options of the outcome. All the option names have be different. The identifier at the end, after the `default` keyword, if present, is the default option. It has to be one of the options listed.

### 1.3.3.2 Spectrums
For more information on spectrums, see [Spectrums](01_02_Symbols.md#1232-spectrums)

The syntax for spectrum declarations is the following:

```cs
SpectrumDeclaration : 'spectrum' identifier '(' (SpectrumOption ',')* identifier ','? ')' ('default' identifier)? ';';
SpectrumOption : identifier ('<' | '<=') integer_literal '/' integer_literal;
```

Since this is one of the more abstract definitions, see an example: `spectrum RelationShip(Apart < 3/10, Neutral <= 7/10, Close);`

The identifier at the beginning, after the `spectrum` keyword is the name of the spectrum, and the other identifiers are the names of the options of the spectrum. All the option names have be different. The identifier at the end, after the `default` keyword, if present, is the default option. It has to be one of the options listed.

Each option has an interval. In the example, the interval $I_\text{Apart} = [0, \frac{3}{10})$, the interval $I_\text{Neutral} = [\frac{3}{10}, \frac{7}{10}]$ and the interval $I_\text{Close} = (\frac{7}{10}, 1]$.

Generally, the first option specifies an upper bound $\frac{a}{b}$. Its interval is then either $[0, \frac{a}{b}]$ (iff it is marked inclusive by usage of `<=`) or $[0, \frac{a}{b})$ (iff it is marked exclusive by usage of `<`). The next option with upper bound $\frac{c}{d}$ then gets the interval $(0, \frac{c}{d})$. The lower inclusivity is the opposite one to the previous interval, that is if the previous interval includes the upper bound, this one excludes its lower bound (since that is equal to the upper bound of the previous interval), and the other way around. The final interval is then the upper bound of the second-to-last one, up to $1$. This is why the last option's upper bound need not be specified, as it is always $1$ (inclusive).

This process requires all of the upper bounds to be strictly increasing and be less than 1. There is one exception to this rule. Take the following spectrum: `spectrum X(A < 1/2, B <= 1/2, C);` This is allowed, since the intervals $[0, \frac{1}{2}), [\frac{1}{2}, \frac{1}{2}] = \{ \frac{1}{2} \}, (\frac{1}{2}, 1]$ are all disjoint. The rule here is that two options next to each other are allowed to have the same upper bound, if the former one is exclusive and the latter one is inclusive. This includes the special case `spectrum X(A < 1/1, B);` Note the need for `1/1`, since every bound is represented as a ratio, and this is just an edge case.

The denominators don't have to be identical, as long as the upper bounds increase.

## 1.3.4 Settings
Settings allow the user to adjust the way the story is compiled or the semantics are handled.

The syntax for a setting directive is the following:

```cs
Setting : 'setting' identifier ':' Type | Expression;
```

The identifiers are builtin and grouped into type and expression settings. An unknown setting is therefore a syntax and not a semantic error, and using a type for an expression setting or vice versa is also a syntax error.

The following settings exist:

| Setting name | Kind | Additional expectations | Default value | See |
|-|-|-|-|-|
| `OutputType` | Type | None | `Int` | [Output Statements](01_04_StatementsAndStates.md#142-output-statements) |
| `OptionType` | Type | None | `Int` | ... |
| `StoryName` | Expression | Expression is a string and a valid identifier | `"HistoriaStory"` | ... |
| `Namespace` | Expression | Expression is a string and a `'.'` seperated list of identifiers, also the first identifier is neither `System` nor `Phantonia`, the empty string is permissable | `""` | ... |

## 1.3.5 Binding Rules
Binding refers to the process of assigning an identifier the symbol that is references. For all declarations, they reference the symbol they declare.

All the top level symbols names have to be different. They may also not override the built in symbol names as specified in [Builtin Types](01_02_Symbols.md#1211-builtin-types).

Any top level symbol may reference any other, for example as a type for a record property or in the body of the scene. All referenced types have to exist in the top level under that exact name.
