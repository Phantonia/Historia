using Phantonia.Historia.Language.SemanticAnalysis.BoundTree;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.TopLevel;
using System.CodeDom.Compiler;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class TypeDeclarationsEmitter
{
    public TypeDeclarationsEmitter(StoryNode boundStory, IndentedTextWriter writer)
    {
        this.boundStory = boundStory;
        this.writer = writer;
    }

    private readonly StoryNode boundStory;
    private readonly IndentedTextWriter writer;

    public void GenerateTypeDeclarations()
    {
        foreach (TopLevelNode topLevelNode in boundStory.TopLevelNodes)
        {
            if (topLevelNode is not BoundSymbolDeclarationNode
                {
                    Symbol: Symbol symbol,
                })
            {
                continue;
            }

            switch (symbol)
            {
                case RecordTypeSymbol recordSymbol:
                    GenerateRecordDeclaration(recordSymbol);
                    break;
                case UnionTypeSymbol unionSymbol:
                    GenerateUnionDeclaration(unionSymbol);
                    break;
                case EnumTypeSymbol enumSymbol:
                    GenerateEnumDeclaration(enumSymbol);
                    break;
                case OutcomeSymbol { Public: true } outcomeSymbol:
                    GenerateOutcomeEnum(outcomeSymbol);
                    break;
                default:
                    continue;
            }

            writer.WriteLine();
        }
    }

    private void GenerateRecordDeclaration(RecordTypeSymbol record)
    {
        writer.Write("public readonly struct @");
        writer.Write(record.Name);
        writer.Write(" : global::System.IEquatable<@");
        writer.Write(record.Name);
        writer.WriteLine('>');

        writer.BeginBlock();

        GenerateRecordConstructor(record);
        writer.WriteLine();

        GenerateRecordProperties(record);

        GenerateRecordEquality(record);

        writer.EndBlock(); // struct
    }

    private void GenerateRecordConstructor(RecordTypeSymbol record)
    {
        writer.Write("internal @");
        writer.Write(record.Name);
        writer.Write('(');

        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            GeneralEmission.GenerateType(property.Type, writer);
            writer.Write(" @");
            writer.Write(property.Name);
            writer.Write(", ");
        }

        GeneralEmission.GenerateType(record.Properties[^1].Type, writer);
        writer.Write(" @");
        writer.Write(record.Properties[^1].Name);
        writer.WriteLine(')');

        writer.BeginBlock();

        foreach (PropertySymbol property in record.Properties)
        {
            writer.Write("this.@");
            writer.Write(property.Name);
            writer.Write(" = @");
            writer.Write(property.Name);
            writer.Write(';');
        }

        writer.WriteLine();
        writer.EndBlock(); // constructor
    }

    private void GenerateRecordProperties(RecordTypeSymbol record)
    {
        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            writer.Write("public ");
            GeneralEmission.GenerateType(property.Type, writer);
            writer.Write(" @");
            writer.Write(property.Name);
            writer.WriteLine(" { get; }");
            writer.WriteLine();
        }

        writer.Write("public ");
        GeneralEmission.GenerateType(record.Properties[^1].Type, writer);
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
        writer.BeginBlock();
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
        writer.EndBlock(); // Equals method
        writer.WriteLine();

        // object.Equals()
        writer.WriteLine("public override bool Equals(object? other)");
        writer.BeginBlock();
        writer.Write("return other is @");
        writer.Write(record.Name);
        writer.WriteLine(" record && Equals(record);");
        writer.EndBlock(); // Equals method
        writer.WriteLine();

        writer.WriteLine("public override int GetHashCode()");
        writer.BeginBlock();
        writer.WriteLine("global::System.HashCode hashcode = default;");

        foreach (PropertySymbol property in record.Properties)
        {
            writer.Write("hashcode.Add(@");
            writer.Write(property.Name);
            writer.WriteLine(");");
        }

        writer.WriteLine("return hashcode.ToHashCode();");
        writer.EndBlock(); // GetHashCode method
        writer.WriteLine();

        // == operator
        writer.Write("public static bool operator ==(@");
        writer.Write(record.Name);
        writer.Write(" x, @");
        writer.Write(record.Name);
        writer.WriteLine(" y)");
        writer.BeginBlock();
        writer.WriteLine("return x.Equals(y);");
        writer.EndBlock(); // == operator
        writer.WriteLine();

        // != operator
        writer.Write("public static bool operator !=(@");
        writer.Write(record.Name);
        writer.Write(" x, @");
        writer.Write(record.Name);
        writer.WriteLine(" y)");
        writer.BeginBlock();
        writer.WriteLine("return !x.Equals(y);");
        writer.EndBlock(); // != operator
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
        writer.BeginBlock();

        GenerateUnionConstructors(union);
        GenerateUnionProperties(union);
        GenerateUnionAsObjectMethod(union);
        GenerateUnionRunMethod(union);
        GenerateUnionEvaluateMethod(union);
        GenerateUnionEquality(union);
        GenerateUnionDiscriminatorEnum(union);
        GenerateUnionExplicitInterfaceImplementations(union);

        writer.EndBlock(); // type
    }

    private void GenerateUnionExplicitInterfaceImplementations(UnionTypeSymbol union)
    {
        if (union.Subtypes.Length is >= 2 and <= 10)
        {
            for (int i = 0; i < union.Subtypes.Length; i++)
            {
                GeneralEmission.GenerateType(union.Subtypes[i], writer);
                writer.Write(' ');
                GenerateUnionInterfaceName(union);
                writer.Write(".Value");
                writer.WriteLine(i);
                writer.BeginBlock();

                writer.WriteLine("get");
                writer.BeginBlock();

                writer.Write("return this.");
                GenerateUnionSubtypeName(union.Subtypes[i]);
                writer.WriteLine(';');

                writer.EndBlock(); // get

                writer.EndBlock(); // Value property

                writer.WriteLine();
            }

            writer.Write("int ");
            GenerateUnionInterfaceName(union);
            writer.WriteLine(".Discriminator");
            writer.BeginBlock();

            writer.WriteLine("get");
            writer.BeginBlock();

            writer.WriteLine("return (int)Discriminator;");

            writer.EndBlock(); // Discriminator.get

            writer.EndBlock(); // property Discriminator
        }
    }

    private void GenerateUnionDiscriminatorEnum(UnionTypeSymbol union)
    {
        writer.Write("public enum ");
        writer.Write(union.Name);
        writer.WriteLine("Discriminator");
        writer.BeginBlock();

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(',');
        }

        writer.EndBlock(); // enum
        writer.WriteLine();
    }

    private void GenerateUnionEquality(UnionTypeSymbol union)
    {
        // IEquatable<union>.Equals()
        writer.Write("public bool Equals(@");
        writer.Write(union.Name);
        writer.WriteLine(" other)");
        writer.BeginBlock();
        writer.Write("return Discriminator == other.Discriminator");

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write(" && this.");
            GenerateUnionSubtypeName(subtype);
            writer.Write(" == other.");
            GenerateUnionSubtypeName(subtype);
        }

        writer.WriteLine(';');
        writer.EndBlock(); // Equals method
        writer.WriteLine();

        // object.Equals()
        writer.WriteLine("public override bool Equals(object? other)");
        writer.BeginBlock();
        writer.Write("return other is @");
        writer.Write(union.Name);
        writer.WriteLine(" union && Equals(union);");
        writer.EndBlock(); // Equals method
        writer.WriteLine();

        // GetHashCode()
        writer.WriteLine("public override int GetHashCode()");
        writer.BeginBlock();
        writer.WriteLine("global::System.HashCode hashcode = default;");

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write("hashcode.Add(this.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(");");
        }

        writer.WriteLine("return hashcode.ToHashCode();");
        writer.EndBlock(); // GetHashCode method
        writer.WriteLine();

        // == operator
        writer.Write("public static bool operator ==(@");
        writer.Write(union.Name);
        writer.Write(" x, @");
        writer.Write(union.Name);
        writer.WriteLine(" y)");
        writer.BeginBlock();
        writer.WriteLine("return x.Equals(y);");
        writer.EndBlock(); // == operator
        writer.WriteLine();

        // != operator
        writer.Write("public static bool operator !=(@");
        writer.Write(union.Name);
        writer.Write(" x, @");
        writer.Write(union.Name);
        writer.WriteLine(" y)");
        writer.BeginBlock();
        writer.WriteLine("return !x.Equals(y);");
        writer.EndBlock(); // != operator
        writer.WriteLine();
    }

    private void GenerateUnionEvaluateMethod(UnionTypeSymbol union)
    {
        writer.Write("public T Evaluate<T>(");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            writer.Write("global::System.Func<");
            GeneralEmission.GenerateType(subtype, writer);
            writer.Write(", T> function");
            GenerateUnionSubtypeName(subtype, includeAt: false);
            writer.Write(", ");
        }

        writer.Write("global::System.Func<");
        GeneralEmission.GenerateType(union.Subtypes[^1], writer);
        writer.Write(", T> function");
        GenerateUnionSubtypeName(union.Subtypes[^1], includeAt: false);
        writer.WriteLine(')');
        writer.BeginBlock();
        writer.WriteLine("switch (Discriminator)");
        writer.BeginBlock();

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

        writer.EndBlock(); // switch
        writer.WriteLine();
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Invalid discriminator\");");
        writer.EndBlock(); // evaluate method
        writer.WriteLine();
    }

    private void GenerateUnionRunMethod(UnionTypeSymbol union)
    {
        writer.Write("public void Run(");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            writer.Write("global::System.Action<");
            GeneralEmission.GenerateType(subtype, writer);
            writer.Write("> action");
            GenerateUnionSubtypeName(subtype, includeAt: false);
            writer.Write(", ");
        }

        writer.Write("global::System.Action<");
        GeneralEmission.GenerateType(union.Subtypes[^1], writer);
        writer.Write("> action");
        GenerateUnionSubtypeName(union.Subtypes[^1], includeAt: false);
        writer.WriteLine(')');

        writer.BeginBlock();
        writer.WriteLine("switch (Discriminator)");
        writer.BeginBlock();

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

        writer.EndBlock(); // switch
        writer.WriteLine();
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Invalid discriminator\");");
        writer.EndBlock(); // Run method
        writer.WriteLine();
    }

    private void GenerateUnionAsObjectMethod(UnionTypeSymbol union)
    {
        writer.WriteLine("public object? AsObject()");
        writer.BeginBlock();
        writer.WriteLine("switch (Discriminator)");
        writer.BeginBlock();

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

        writer.EndBlock(); // switch
        writer.WriteLine();
        writer.WriteLine("throw new global::System.InvalidOperationException(\"Invalid discriminator\");");
        writer.EndBlock(); // AsObject method
        writer.WriteLine();
    }

    private void GenerateUnionProperties(UnionTypeSymbol union)
    {
        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write("public ");
            GeneralEmission.GenerateType(subtype, writer);
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
            GeneralEmission.GenerateType(subtype, writer);
            writer.WriteLine(" value)");
            writer.BeginBlock();
            writer.Write("this.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(" = value;");
            writer.Write("Discriminator = ");
            writer.Write(union.Name);
            writer.Write("Discriminator.");
            GenerateUnionSubtypeName(subtype);
            writer.WriteLine(';');
            writer.EndBlock(); // constructor
            writer.WriteLine();
        }
    }

    private void GenerateUnionInterfaceName(UnionTypeSymbol union)
    {
        writer.Write("global::Phantonia.Historia.IUnion<");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            GeneralEmission.GenerateType(subtype, writer);
            writer.Write(", ");
        }

        GeneralEmission.GenerateType(union.Subtypes[^1], writer);
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
        writer.Write("public enum @");
        writer.WriteLine(enumSymbol.Name);
        writer.BeginBlock();

        foreach (string option in enumSymbol.Options)
        {
            writer.Write(option);
            writer.WriteLine(',');
        }

        writer.EndBlock(); // enum
    }

    private void GenerateOutcomeEnum(OutcomeSymbol outcomeSymbol)
    {
        writer.Write("public enum ");

        if (outcomeSymbol is SpectrumSymbol)
        {
            writer.Write("Spectrum");
        }
        else
        {
            writer.Write("Outcome");
        }

        writer.WriteLine(outcomeSymbol.Name);
        writer.BeginBlock();

        if (outcomeSymbol.DefaultOption is null)
        {
            int i = 0;

            while (outcomeSymbol.OptionNames.Contains(new string('_', i) + "Unset"))
            {
                i++;
            }

            for (int j = 0; j < i; j++)
            {
                writer.Write('_');
            }

            writer.WriteLine("Unset = 0,");
        }

        foreach (string option in outcomeSymbol.OptionNames)
        {
            writer.Write(option);
            writer.WriteLine(',');
        }

        writer.EndBlock(); // enum
    }
}
