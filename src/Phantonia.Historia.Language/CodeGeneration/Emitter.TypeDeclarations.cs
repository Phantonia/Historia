using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed partial class Emitter
{
    private void GenerateTypeDeclarations()
    {
        foreach (TopLevelNode topLevelNode in boundStory.TopLevelNodes)
        {
            if (topLevelNode is not BoundSymbolDeclarationNode
                {
                    Declaration: TypeSymbolDeclarationNode,
                    Symbol: TypeSymbol symbol,
                })
            {
                continue;
            }

            switch (symbol)
            {
                case RecordTypeSymbol recordSymbol:
                    GenerateRecordDeclaration(recordSymbol);
                    continue;
                case UnionTypeSymbol unionSymbol:
                    GenerateUnionDeclaration(unionSymbol);
                    continue;
                case EnumTypeSymbol enumSymbol:
                    GenerateEnumDeclaration(enumSymbol);
                    continue;
                default:
                    Debug.Assert(false);
                    return;
            }
        }
    }

    private void GenerateRecordDeclaration(RecordTypeSymbol record)
    {
        writer.Write("public readonly struct @");
        writer.Write(record.Name);
        writer.Write(" : global::System.IEquatable<@");
        writer.Write(record.Name);
        writer.WriteLine('>');

        writer.WriteLine('{');

        writer.Indent++;

        GenerateRecordConstructor(record);
        writer.WriteLine();

        GenerateRecordProperties(record);

        GenerateRecordEquality(record);

        writer.Indent--;
        writer.WriteLine('}');
    }

    private void GenerateRecordConstructor(RecordTypeSymbol record)
    {
        writer.Write("internal @");
        writer.Write(record.Name);
        writer.Write('(');

        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            GenerateType(property.Type);
            writer.Write(" @");
            writer.Write(property.Name);
            writer.Write(", ");
        }

        GenerateType(record.Properties[^1].Type);
        writer.Write(" @");
        writer.Write(record.Properties[^1].Name);
        writer.WriteLine(')');

        writer.WriteLine('{');

        writer.Indent++;

        foreach (PropertySymbol property in record.Properties)
        {
            writer.Write("this.@");
            writer.Write(property.Name);
            writer.Write(" = @");
            writer.Write(property.Name);
            writer.Write(';');
        }

        writer.Indent--;
        writer.WriteLine('}');
    }

    private void GenerateRecordProperties(RecordTypeSymbol record)
    {
        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            writer.Write("public ");
            GenerateType(property.Type);
            writer.Write(" @");
            writer.Write(property.Name);
            writer.WriteLine(" { get; }");
            writer.WriteLine();
        }

        writer.Write("public ");
        GenerateType(record.Properties[^1].Type);
        writer.Write(" @");
        writer.Write(record.Properties[^1].Name);
        writer.WriteLine(" { get; }");
        writer.WriteLine();
    }

    private void GenerateRecordEquality(RecordTypeSymbol record)
    {
        // IEquatable<record>.Equals()
        writer.Write("public bool Equals(@");
        writer.Write(record.Name);
        writer.WriteLine(" other)");
        writer.WriteLine('{');
        writer.Indent++;
        writer.Write("return ");

        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            writer.Write('@');
            writer.Write(property.Name);
            writer.Write(" == other.@");
            writer.Write(property.Name);
            writer.Write(" && ");
        }

        writer.Write('@');
        writer.Write(record.Properties[^1].Name);
        writer.Write(" == other.@");
        writer.Write(record.Properties[^1].Name);
        writer.WriteLine(';');
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        // object.Equals()
        writer.WriteLine("public override bool Equals(object? other)");
        writer.WriteLine('{');
        writer.Indent++;
        writer.Write("return other is @");
        writer.Write(record.Name);
        writer.WriteLine(" record && Equals(record);");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        writer.WriteLine("public override int GetHashCode()");
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("global::System.HashCode hashcode = default;");

        foreach (PropertySymbol property in record.Properties)
        {
            writer.Write("hashcode.Add(@");
            writer.Write(property.Name);
            writer.WriteLine(");");
        }

        writer.WriteLine("return hashcode.ToHashCode();");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        // == operator
        writer.Write("public static bool operator ==(@");
        writer.Write(record.Name);
        writer.Write(" x, @");
        writer.Write(record.Name);
        writer.WriteLine(" y)");
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("return x.Equals(y);");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        // != operator
        writer.Write("public static bool operator !=(@");
        writer.Write(record.Name);
        writer.Write(" x, @");
        writer.Write(record.Name);
        writer.WriteLine(" y)");
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("return !x.Equals(y);");
        writer.Indent--;
        writer.WriteLine('}');
    }

    private void GenerateUnionDeclaration(UnionTypeSymbol union)
    {
        writer.Write("public readonly struct @");
        writer.Write(union.Name);
        writer.Write(" : global::System.IEquatable<@");
        writer.Write(union.Name);
        writer.Write('>');

        if (union.Subtypes.Length is >= 2 and <= 10)
        {
            // the IUnion interface only exists for those
            writer.Write(", ");
            GenerateUnionInterfaceName(union);
        }

        writer.WriteLine();
        writer.WriteLine('{');

        writer.Indent++;

        GenerateUnionConstructors(union);
        GenerateUnionProperties(union);
        GenerateUnionAsObjectMethod(union);
        GenerateUnionRunMethod(union);
        GenerateUnionEvaluateMethod(union);
        GenerateUnionEquality(union);
        GenerateUnionDiscriminatorEnum(union);
        GenerateUnionExplicitInterfaceImplementations(union);
    }

    private void GenerateUnionExplicitInterfaceImplementations(UnionTypeSymbol union)
    {
        if (union.Subtypes.Length is >= 2 and <= 10)
        {
            for (int i = 0; i < union.Subtypes.Length; i++)
            {
                GenerateType(union.Subtypes[i]);
                writer.Write(' ');
                GenerateUnionInterfaceName(union);
                writer.Write(".Value");
                writer.WriteLine(i);
                writer.WriteLine('{');

                writer.Indent++;

                writer.WriteLine("get");
                writer.WriteLine('{');

                writer.Indent++;

                writer.Write("return this.");
                GenerateUnionSubtypeName(union.Subtypes[i]);
                writer.WriteLine(';');

                writer.Indent--;

                writer.WriteLine('}');

                writer.Indent--;
                writer.WriteLine('}');

                writer.WriteLine();
            }

            writer.Write("int ");
            GenerateUnionInterfaceName(union);
            writer.WriteLine(".Discriminator");
            writer.WriteLine('{');

            writer.Indent++;

            writer.WriteLine("get");
            writer.WriteLine('{');

            writer.Indent++;

            writer.WriteLine("return (int)Discriminator;");

            writer.Indent--;
            writer.WriteLine('}');

            writer.Indent--;
            writer.WriteLine('}');

            writer.Indent--;
            writer.WriteLine('}');
        }
    }

    private void GenerateUnionDiscriminatorEnum(UnionTypeSymbol union)
    {
        writer.Write("public enum ");
        writer.Write(union.Name);
        writer.WriteLine("Discriminator");
        writer.WriteLine('{');
        writer.Indent++;

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(',');
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
    }

    private void GenerateUnionEquality(UnionTypeSymbol union)
    {
        // IEquatable<union>.Equals()
        writer.Write("public bool Equals(@");
        writer.Write(union.Name);
        writer.WriteLine(" other)");
        writer.WriteLine('{');
        writer.Indent++;
        writer.Write("return Discriminator == other.Discriminator");

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write(" && this.");
            GenerateUnionSubtypeName(subtype);
            writer.Write(" == other.");
            GenerateUnionSubtypeName(subtype);
        }

        writer.WriteLine(';');
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        // object.Equals()
        writer.WriteLine("public override bool Equals(object? other)");
        writer.WriteLine('{');
        writer.Indent++;
        writer.Write("return other is @");
        writer.Write(union.Name);
        writer.WriteLine(" union && Equals(union);");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        // GetHashCode()
        writer.WriteLine("public override int GetHashCode()");
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("global::System.HashCode hashcode = default;");

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write("hashcode.Add(this.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(");");
        }

        writer.WriteLine("return hashcode.ToHashCode();");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        // == operator
        writer.Write("public static bool operator ==(@");
        writer.Write(union.Name);
        writer.Write(" x, @");
        writer.Write(union.Name);
        writer.WriteLine(" y)");
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("return x.Equals(y);");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        // != operator
        writer.Write("public static bool operator !=(@");
        writer.Write(union.Name);
        writer.Write(" x, @");
        writer.Write(union.Name);
        writer.WriteLine(" y)");
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("return !x.Equals(y);");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
    }

    private void GenerateUnionEvaluateMethod(UnionTypeSymbol union)
    {
        writer.Write("public T Evaluate<T>(");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            writer.Write("global::System.Func<");
            GenerateType(subtype);
            writer.Write(", T> function");
            GenerateUnionSubtypeName(subtype, includeAt: false);
            writer.Write(", ");
        }

        writer.Write("global::System.Func<");
        GenerateType(union.Subtypes[^1]);
        writer.Write(", T> function");
        GenerateUnionSubtypeName(union.Subtypes[^1], includeAt: false);
        writer.WriteLine(')');
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("switch (Discriminator)");
        writer.WriteLine('{');
        writer.Indent++;

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write("case ");
            writer.Write(union.Name);
            writer.Write("Discriminator.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(':');
            writer.Indent++;
            writer.Write("return function");
            GenerateUnionSubtypeName(subtype, includeAt: false);
            writer.Write("(this.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(");");
            writer.Indent--;
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Invalid discriminator\");");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
    }

    private void GenerateUnionRunMethod(UnionTypeSymbol union)
    {
        writer.Write("public void Run(");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            writer.Write("global::System.Action<");
            GenerateType(subtype);
            writer.Write("> action");
            GenerateUnionSubtypeName(subtype, includeAt: false);
            writer.Write(", ");
        }

        writer.Write("global::System.Action<");
        GenerateType(union.Subtypes[^1]);
        writer.Write("> action");
        GenerateUnionSubtypeName(union.Subtypes[^1], includeAt: false);
        writer.WriteLine(')');

        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("switch (Discriminator)");
        writer.WriteLine('{');
        writer.Indent++;

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write("case ");
            writer.Write(union.Name);
            writer.Write("Discriminator.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(':');
            writer.Indent++;
            writer.Write("action");
            GenerateUnionSubtypeName(subtype, includeAt: false);
            writer.Write("(this.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(");");
            writer.WriteLine("return;");
            writer.Indent--;
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Invalid discriminator\");");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
    }

    private void GenerateUnionAsObjectMethod(UnionTypeSymbol union)
    {
        writer.WriteLine("public object? AsObject()");
        writer.WriteLine('{');
        writer.Indent++;
        writer.WriteLine("switch (Discriminator)");
        writer.WriteLine('{');
        writer.Indent++;

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write("case ");
            writer.Write(union.Name);
            writer.Write("Discriminator.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(':');
            writer.Indent++;
            writer.Write("return this.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(';');
            writer.Indent--;
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Invalid discriminator\");");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
    }

    private void GenerateUnionProperties(UnionTypeSymbol union)
    {
        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write("public ");
            GenerateType(subtype);
            writer.Write(' ');
            GenerateUnionSubtypeName(subtype); // the property gets the same name as the type
            writer.WriteLine(" { get; }");
            writer.WriteLine();
        }

        writer.Write("public ");
        writer.Write(union.Name);
        writer.WriteLine("Discriminator Discriminator { get; }");
        writer.WriteLine();
    }

    private void GenerateUnionConstructors(UnionTypeSymbol union)
    {
        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write($"internal @{union.Name}(");
            GenerateType(subtype);
            writer.WriteLine(" value)");
            writer.WriteLine('{');
            writer.Indent++;
            writer.Write("this.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(" = value;");
            writer.Write("Discriminator = ");
            writer.Write(union.Name);
            writer.Write("Discriminator.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(';');
            writer.Indent--;
            writer.WriteLine('}');
            writer.WriteLine();
        }
    }

    private void GenerateUnionInterfaceName(UnionTypeSymbol union)
    {
        writer.Write("global::Phantonia.Historia.IUnion<");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            GenerateType(subtype);
            writer.Write(", ");
        }

        GenerateType(union.Subtypes[^1]);
        writer.Write('>');
    }

    private void GenerateUnionSubtypeName(TypeSymbol subtype, bool includeAt = true)
    {
        if (includeAt)
        {
            writer.Write('@');
        }

        writer.Write(subtype.Name);
    }

    private void GenerateEnumDeclaration(PseudoEnumTypeSymbol enumSymbol)
    {
        writer.Write("public enum ");
        writer.WriteLine(enumSymbol.Name);
        writer.WriteLine('{');

        writer.Indent++;

        foreach (string option in enumSymbol.Options)
        {
            writer.Write(option);
            writer.WriteLine(',');
        }

        writer.Indent--;

        writer.WriteLine('}');
    }
}
