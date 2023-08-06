using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed partial class Emitter
{
    private void GenerateTypeDeclarations(IndentedTextWriter writer)
    {
        foreach (TopLevelNode topLevelNode in boundStory.TopLevelNodes)
        {
            if (topLevelNode is not BoundSymbolDeclarationNode
                {
                    Declaration: TypeSymbolDeclarationNode declaration,
                    Symbol: TypeSymbol symbol,
                })
            {
                continue;
            }

            switch (symbol)
            {
                case RecordTypeSymbol recordSymbol:
                    GenerateRecordDeclaration(writer, recordSymbol);
                    continue;
                case UnionTypeSymbol unionSymbol:
                    GenerateUnionDeclaration(writer, unionSymbol);
                    continue;
                default:
                    Debug.Assert(false);
                    return;
            }
        }
    }

    private void GenerateRecordDeclaration(IndentedTextWriter writer, RecordTypeSymbol record)
    {
        writer.Write("public readonly struct @");
        writer.Write(record.Name);
        writer.Write(" : global::System.IEquatable<@");
        writer.Write(record.Name);
        writer.WriteLine('>');

        writer.WriteLine('{');

        writer.Indent++;

        GenerateRecordConstructor(writer, record);
        writer.WriteLine();

        GenerateRecordProperties(writer, record);

        GenerateRecordEquality(writer, record);

        writer.Indent--;
        writer.WriteLine('}');
    }

    private void GenerateRecordConstructor(IndentedTextWriter writer, RecordTypeSymbol record)
    {
        writer.Write("internal @");
        writer.Write(record.Name);
        writer.Write('(');

        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            GenerateType(writer, property.Type);
            writer.Write(" @");
            writer.Write(property.Name);
            writer.Write(", ");
        }

        GenerateType(writer, record.Properties[^1].Type);
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

    private void GenerateRecordProperties(IndentedTextWriter writer, RecordTypeSymbol record)
    {
        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            writer.Write("public ");
            GenerateType(writer, property.Type);
            writer.Write(" @");
            writer.Write(property.Name);
            writer.WriteLine(" { get; }");
            writer.WriteLine();
        }

        writer.Write("public ");
        GenerateType(writer, record.Properties[^1].Type);
        writer.Write(" @");
        writer.Write(record.Properties[^1].Name);
        writer.WriteLine(" { get; }");
        writer.WriteLine();
    }

    private void GenerateRecordEquality(IndentedTextWriter writer, RecordTypeSymbol record)
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

    private void GenerateUnionDeclaration(IndentedTextWriter writer, UnionTypeSymbol union)
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
            GenerateUnionInterfaceName(writer, union);
        }

        writer.WriteLine();
        writer.WriteLine('{');

        writer.Indent++;

        GenerateUnionConstructors(writer, union);
        GenerateUnionProperties(writer, union);
        GenerateUnionAsObjectMethod(writer, union);
        GenerateUnionRunMethod(writer, union);
        GenerateUnionEvaluateMethod(writer, union);
        GenerateUnionEquality(writer, union);
        GenerateUnionDiscriminatorEnum(writer, union);
        GenerateUnionExplicitInterfaceImplementations(writer, union);
    }

    private void GenerateUnionExplicitInterfaceImplementations(IndentedTextWriter writer, UnionTypeSymbol union)
    {
        if (union.Subtypes.Length is >= 2 and <= 10)
        {
            for (int i = 0; i < union.Subtypes.Length; i++)
            {
                GenerateType(writer, union.Subtypes[i]);
                writer.Write(' ');
                GenerateUnionInterfaceName(writer, union);
                writer.Write(".Value");
                writer.WriteLine(i);
                writer.WriteLine('{');

                writer.Indent++;

                writer.WriteLine("get");
                writer.WriteLine('{');

                writer.Indent++;

                writer.Write("return this.");
                GenerateUnionSubtypeName(writer, union.Subtypes[i]);
                writer.WriteLine(';');

                writer.Indent--;

                writer.WriteLine('}');

                writer.Indent--;
                writer.WriteLine('}');

                writer.WriteLine();
            }

            writer.Write("int ");
            GenerateUnionInterfaceName(writer, union);
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

    private void GenerateUnionDiscriminatorEnum(IndentedTextWriter writer, UnionTypeSymbol union)
    {
        writer.Write("public enum ");
        writer.Write(union.Name);
        writer.WriteLine("Discriminator");
        writer.WriteLine('{');
        writer.Indent++;

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            GenerateUnionSubtypeName(writer, subtype);
            writer.WriteLine(',');
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();
    }

    private void GenerateUnionEquality(IndentedTextWriter writer, UnionTypeSymbol union)
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
            GenerateUnionSubtypeName(writer, subtype);
            writer.Write(" == other.");
            GenerateUnionSubtypeName(writer, subtype);
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
            GenerateUnionSubtypeName(writer, subtype);
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

    private void GenerateUnionEvaluateMethod(IndentedTextWriter writer, UnionTypeSymbol union)
    {
        writer.Write("public T Evaluate<T>(");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            writer.Write("global::System.Func<");
            GenerateType(writer, subtype);
            writer.Write(", T> function");
            GenerateUnionSubtypeName(writer, subtype, includeAt: false);
            writer.Write(", ");
        }

        writer.Write("global::System.Func<");
        GenerateType(writer, union.Subtypes[^1]);
        writer.Write(", T> function");
        GenerateUnionSubtypeName(writer, union.Subtypes[^1], includeAt: false);
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
            GenerateUnionSubtypeName(writer, subtype);
            writer.WriteLine(':');
            writer.Indent++;
            writer.Write("return function");
            GenerateUnionSubtypeName(writer, subtype, includeAt: false);
            writer.Write("(this.");
            GenerateUnionSubtypeName(writer, subtype);
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

    private void GenerateUnionRunMethod(IndentedTextWriter writer, UnionTypeSymbol union)
    {
        writer.Write("public void Run(");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            writer.Write("global::System.Action<");
            GenerateType(writer, subtype);
            writer.Write("> action");
            GenerateUnionSubtypeName(writer, subtype, includeAt: false);
            writer.Write(", ");
        }

        writer.Write("global::System.Action<");
        GenerateType(writer, union.Subtypes[^1]);
        writer.Write("> action");
        GenerateUnionSubtypeName(writer, union.Subtypes[^1], includeAt: false);
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
            GenerateUnionSubtypeName(writer, subtype);
            writer.WriteLine(':');
            writer.Indent++;
            writer.Write("action");
            GenerateUnionSubtypeName(writer, subtype, includeAt: false);
            writer.Write("(this.");
            GenerateUnionSubtypeName(writer, subtype);
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

    private void GenerateUnionAsObjectMethod(IndentedTextWriter writer, UnionTypeSymbol union)
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
            GenerateUnionSubtypeName(writer, subtype);
            writer.WriteLine(':');
            writer.Indent++;
            writer.Write("return this.");
            GenerateUnionSubtypeName(writer, subtype);
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

    private void GenerateUnionProperties(IndentedTextWriter writer, UnionTypeSymbol union)
    {
        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write("public ");
            GenerateType(writer, subtype);
            writer.Write(' ');
            GenerateUnionSubtypeName(writer, subtype); // the property gets the same name as the type
            writer.WriteLine(" { get; }");
            writer.WriteLine();
        }

        writer.Write("public ");
        writer.Write(union.Name);
        writer.WriteLine("Discriminator Discriminator { get; }");
        writer.WriteLine();
    }

    private void GenerateUnionConstructors(IndentedTextWriter writer, UnionTypeSymbol union)
    {
        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write($"internal @{union.Name}(");
            GenerateType(writer, subtype);
            writer.WriteLine(" value)");
            writer.WriteLine('{');
            writer.Indent++;
            writer.Write("this.");
            GenerateUnionSubtypeName(writer, subtype);
            writer.WriteLine(" = value;");
            writer.Write("Discriminator = ");
            writer.Write(union.Name);
            writer.Write("Discriminator.");
            GenerateUnionSubtypeName(writer, subtype);
            writer.WriteLine(';');
            writer.Indent--;
            writer.WriteLine('}');
            writer.WriteLine();
        }
    }

    private void GenerateUnionInterfaceName(IndentedTextWriter writer, UnionTypeSymbol union)
    {
        writer.Write("global::Phantonia.Historia.IUnion<");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            GenerateType(writer, subtype);
            writer.Write(", ");
        }

        GenerateType(writer, union.Subtypes[^1]);
        writer.Write('>');
    }

    private void GenerateUnionSubtypeName(IndentedTextWriter writer, TypeSymbol subtype, bool includeAt = true)
    {
        if (includeAt)
        {
            writer.Write('@');
        }

        writer.Write(subtype.Name);
    }
}
