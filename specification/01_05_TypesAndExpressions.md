# 1.5 Types and Expression

## 1.5.1 Types
A type is a set of values. Historia only contains named types, which include the builtin types and types declared at the top level, see [Types](01_02_Symbols.md#121-type-symbols).

When referencing a type, the following syntax is used:

```cs
Type : identifier;
```

This identifier has to be of a type symbol in scope.

## 1.5.2 Expressions
Expression produce values. These values have a so-called <u>minimal type</u>, which is specified in their respective sections. More information on non-minimal types later in [Conversion Rules]().

The general syntax for expressions is

```cs
Expression : integer_literal
           | string_literal
           | RecordCreationExpression
           | EnumOptionExpression
           ;
```

### 1.5.2.1 Integer Literals
An integer literal references a 32-bit integer.

The syntax is the following:

```cs
integer_literal : '-'? ('0'..'9')+;
```

(Note that the casing of `integer_literal` indicates that this is a token, not a node. No spacing is allowed here between any of the characters.)

The resulting literal is parsed base 10 and has to in the interval $[-2^{31}, 2^{31})$, else an error is produced.

The expression's minimal type is the builtin type `Int`.

### 1.5.2.2 String Literals
A string literal is a sequence of UTF-16 characters. The Historia language completely copies the way C# handles classic string literals (see that specification), with a few exceptions:

A string literal may start with one of `'"'+` or `'\''+` and must end with the identical lexeme. That is, it may start with `"`, `'`, `"""`, `''` or any number of quotation marks, and must be terminated exactly like it is started. Inside of the literal, the quotation mark that is used may be used together in a quantity less than used to delimit the string.

Also, Historia does not support `\x` escape sequences.

### 1.5.2.3 Record Creations
Record creations specify a value for each of a record's properties, and in return produce a value of this record type.

The syntax is as follows:

```cs
RecordCreationExpression : identifier '(' RecordCreationProperty (',' RecordCreationProperty)? ',' ')';
RecordCreationProperty : (identifier '=')? Expression;
```

The identifier at the beginning must refer to a record symbol in scope. The order of the properties has to match the order of the properties in the record declaration exactly, even when the property is named (i.e. it has the `identifier '='`) part. In that case, the identifier has to match the property name exactly.

For each of the expression, their minimal type has to be compatible with the declared type of the property.

The expression's minimal type is the record symbol referenced at the beginning.

### 1.5.2.4 Enum Option Expressions
An enum option expressions choose one of the options of an enum.

The syntax is as follows:

```cs
EnumOptionExpression : identifier '.' identifier;
```

The first identifier has to refer to an enum symbol in scope, and the second identifier has to refer to an option declared on that enum symbol.

The expression's minimal type is the enum type referenced.

## 1.5.3 Type Compatibility
This section formalises the idea of "This expression is compatible with that type". Given an expression with minimal type $A$, from now on called the <u>source type</u>. For type compatibility, a second type is also required, the <u>target type</u> $B$.

Source type $A$ is compatible with target type $B$, iff $A = B$, or $B$ is a union type and $A$ is a subtype of that union. This includes the case, where $A$ is itself a union, and the set of non-union subtypes on $A$ is a subset of the set of non-union subtypes on $B$.

Note that the relation "is compatible with" is not symmetric, i.e. it matters which one is the source and target type, and basically amounts to $A \subseteq B$.
