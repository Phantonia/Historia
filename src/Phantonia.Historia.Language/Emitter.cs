// uncomment this to see the example story class below
// #define EXAMPLE_STORY

using Microsoft.CodeAnalysis.CSharp;
using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis;
using Phantonia.Historia.Language.GrammaticalAnalysis.Expressions;
using Phantonia.Historia.Language.GrammaticalAnalysis.Statements;
using Phantonia.Historia.Language.GrammaticalAnalysis.TopLevel;
using Phantonia.Historia.Language.SemanticAnalysis;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phantonia.Historia.Language;

public sealed class Emitter
{
    public Emitter(StoryNode boundStory, Settings settings, FlowGraph flowGraph)
    {
        this.boundStory = boundStory;
        this.settings = settings;
        this.flowGraph = flowGraph;
    }

    private readonly StoryNode boundStory;
    private readonly Settings settings;
    private readonly FlowGraph flowGraph;

    public string GenerateCSharpText()
    {
        IndentedTextWriter writer = new(new StringWriter());

        writer.WriteLine("#nullable enable");
        writer.Write("public sealed class @");
        writer.Write(settings.ClassName);
        writer.Write(" : global::Phantonia.Historia.IStory<");
        GenerateType(writer, settings.OutputType);
        writer.Write(", ");
        GenerateType(writer, settings.OptionType);
        writer.WriteLine('>');
        writer.WriteLine('{');

        writer.Indent++;

        writer.Write("public @");
        writer.Write(settings.ClassName);
        writer.WriteLine("()");
        writer.WriteLine('{');

        writer.Indent++;

        if (flowGraph.StartVertex == FlowGraph.EmptyVertex)
        {
            writer.Write("FinishedStory = true;");
        }
        else
        {
            writer.Write("Output = ");

            if (flowGraph.Vertices[flowGraph.StartVertex].AssociatedStatement is IOutputStatementNode outputStatement)
            {
                GenerateExpression(writer, outputStatement.OutputExpression);
            }
            else
            {
                writer.Write("default");
            }

            writer.WriteLine(";");
        }

        writer.Indent--;

        writer.WriteManyLines(
          $$"""
            }
                      
            private int state = {{flowGraph.StartVertex}};
            """);

        GenerateOutcomeFields(writer);

        writer.WriteLine();

        writer.WriteLine("public bool FinishedStory { get; private set; } = false;");

        writer.WriteLine();

        writer.Write("public global::System.Collections.Immutable.ImmutableArray<");
        GenerateType(writer, settings.OptionType);
        writer.Write("> Options { get; private set; } = global::System.Collections.Immutable.ImmutableArray<");
        GenerateType(writer, settings.OptionType);
        writer.WriteLine(">.Empty;");

        writer.WriteLine();

        writer.Write("public ");
        GenerateType(writer, settings.OutputType);
        writer.WriteLine(" Output { get; private set; }");
        writer.WriteLine();

        writer.WriteManyLines(
            $$"""
            public bool TryContinue()
            {
                if (FinishedStory || Options.Length != 0)
                {
                    return false;
                }

                StateTransition(0);
                Output = GetOutput();
                Options = GetOptions();
            
                if (state == -1)
                {
                    FinishedStory = true;
                }
            
                return true;
            }

            public bool TryContinueWithOption(int option)
            {
                if (FinishedStory || option < 0 || option >= Options.Length)
                {
                    return false;
                }

                StateTransition(option);
                Output = GetOutput();
                Options = GetOptions();

                if (state == -1)
                {
                    FinishedStory = true;
                }

                return true;
            }

            """);

        GenerateStateTransitionMethod(writer);

        writer.WriteLine();

        GenerateGetOutputMethod(writer);

        writer.WriteLine();

        GenerateGetOptionsMethod(writer);

        writer.WriteLine();

        GenerateTypes(writer);

        writer.WriteLine();
        writer.Write("global::System.Collections.Generic.IReadOnlyList<");
        GenerateType(writer, settings.OptionType);
        writer.Write("> global::Phantonia.Historia.IStory<");
        GenerateType(writer, settings.OutputType);
        writer.Write(", ");
        GenerateType(writer, settings.OptionType);
        writer.WriteLine(">.Options");
        writer.WriteManyLines(
            """
            {
                get
                {
                    return Options;
                }
            }
            """);

        writer.Indent--;

        writer.WriteLine("}");

        Debug.Assert(writer.Indent == 0);

        return ((StringWriter)writer.InnerWriter).ToString();
    }

    private void GenerateOutcomeFields(IndentedTextWriter writer)
    {
        foreach (SyntaxNode node in boundStory.FlattenHierarchie())
        {
            if (node is IBoundOutcomeDeclarationNode { Outcome: OutcomeSymbol outcome })
            {
                writer.Write($"private int {GetOutcomeFieldName(outcome)}");

                if (outcome.DefaultOption is not null)
                {
                    writer.Write($" = {outcome.OptionNames.IndexOf(outcome.DefaultOption)}");
                }

                writer.WriteLine(";");
            }
        }
    }

    private void GenerateStateTransitionMethod(IndentedTextWriter writer)
    {
        writer.WriteManyLines(
            """
            private void StateTransition(int option)
            {
                while (true)
                {
                    switch (state, option)
                    {
            """);

        writer.Indent += 3;

        foreach ((int index, ImmutableList<int> edges) in flowGraph.OutgoingEdges)
        {
            switch (flowGraph.Vertices[index].AssociatedStatement)
            {
                case OutputStatementNode:
                    {
                        Debug.Assert(flowGraph.OutgoingEdges[index].Count == 1);

                        writer.WriteLine($"case ({index}, _):");

                        writer.Indent++;
                        writer.WriteLine($"state = {edges[0]};");

                        if (edges[0] == FlowGraph.EmptyVertex || flowGraph.Vertices[edges[0]].IsVisible)
                        {
                            writer.WriteLine("return;");
                        }
                        else
                        {
                            writer.WriteLine("continue;");
                        }

                        writer.Indent--;
                    }
                    break;
                case SwitchStatementNode switchStatement:
                    {
                        Debug.Assert(switchStatement.Options.Length == edges.Count);

                        for (int i = 0; i < switchStatement.Options.Length; i++)
                        {
                            // we know that for each option its index equals that of the associated next vertex
                            // we also know that the order that the options appear in the switch statement node is exactly how each one will be indexed
                            // plus we know that the outgoing edges are in exactly the right order

                            // here we assert that the index of the vertex equals the index of the first statement of the option
                            // however we ignore the case where the option's body is empty - there it would be impossible to get the next statement
                            Debug.Assert(switchStatement.Options[i].Body.Statements.Length == 0 || switchStatement.Options[i].Body.Statements[0].Index == edges[i]);

                            writer.WriteLine($"case ({index}, {i}):");

                            writer.Indent++;
                            writer.WriteLine($"state = {edges[i]};");

                            if (switchStatement is BoundNamedSwitchStatementNode { Outcome: OutcomeSymbol outcome })
                            {
                                writer.WriteLine($"{GetOutcomeFieldName(outcome)} = {outcome.OptionNames.IndexOf(switchStatement.Options[i].Name!)};");
                            }

                            if (edges[i] == FlowGraph.EmptyVertex || flowGraph.Vertices[edges[i]].IsVisible)
                            {
                                writer.WriteLine("return;");
                            }
                            else
                            {
                                writer.WriteLine("continue;");
                            }

                            writer.Indent--;
                        }
                    }
                    break;
                case BoundBranchOnStatementNode branchOnStatement:
                    {
                        writer.WriteLine($"case ({index}, _):");

                        writer.Indent++;
                        writer.WriteLine($"switch ({GetOutcomeFieldName(branchOnStatement.Outcome)})");
                        writer.WriteLine('{');
                        writer.Indent++;

                        for (int i = 0; i < branchOnStatement.Options.Length; i++)
                        {
                            BranchOnOptionNode option = branchOnStatement.Options[i];

                            if (option is NamedBranchOnOptionNode { OptionName: string optionName })
                            {
                                writer.WriteLine($"case {branchOnStatement.Outcome.OptionNames.IndexOf(optionName)}:");
                                writer.Indent++;
                                writer.WriteLine($"state = {edges[i]};");

                                if (edges[i] == FlowGraph.EmptyVertex || flowGraph.Vertices[edges[i]].IsVisible)
                                {
                                    writer.WriteLine("return;");
                                }
                                else
                                {
                                    writer.WriteLine("continue;");
                                }

                                writer.Indent--;
                            }
                            else
                            {
                                writer.WriteLine("default:");
                                writer.Indent++;
                                writer.WriteLine($"state = {flowGraph.OutgoingEdges[branchOnStatement.Index][i]};");

                                if (edges[i] == FlowGraph.EmptyVertex || flowGraph.Vertices[edges[i]].IsVisible)
                                {
                                    writer.WriteLine("return;");
                                }
                                else
                                {
                                    writer.WriteLine("continue;");
                                }
                            }
                        }

                        writer.Indent--;
                        writer.WriteLine('}');
                        writer.WriteLine();
                        writer.WriteLine("throw new global::System.InvalidOperationException(\"Invalid outcome\");");
                        writer.Indent--;
                    }
                    break;
                case BoundOutcomeAssignmentStatementNode outcomeAssignment:
                    writer.WriteLine($"case ({outcomeAssignment.Index}, _):");
                    writer.Indent++;
                    writer.Write(GetOutcomeFieldName(outcomeAssignment.Outcome));
                    writer.Write(" = ");
                    writer.Write(outcomeAssignment.Outcome.OptionNames.IndexOf(outcomeAssignment.AssignedOption));
                    writer.WriteLine(";");
                    writer.WriteLine($"state = {flowGraph.OutgoingEdges[outcomeAssignment.Index][0]};");
                    writer.WriteLine("continue;");
                    writer.Indent--;
                    break;
            }
        }

        writer.Indent -= 3;

        writer.WriteManyLines(
            """
                    }

                    throw new global::System.InvalidOperationException("Invalid state");
                }
            }
            """);

    }

    private void GenerateGetOutputMethod(IndentedTextWriter writer)
    {
        writer.Write("private ");
        GenerateType(writer, settings.OutputType);
        writer.WriteLine(" GetOutput()");

        writer.WriteManyLines(
            $$"""
              {
                  switch (state)
                  {
              """);

        writer.Indent += 2;

        foreach ((int index, FlowVertex vertex) in flowGraph.Vertices)
        {
            ExpressionNode outputExpression;

            switch (vertex.AssociatedStatement)
            {
                case OutputStatementNode outputStatement:
                    outputExpression = outputStatement.OutputExpression;
                    break;
                case SwitchStatementNode switchStatement:
                    outputExpression = switchStatement.OutputExpression;
                    break;
                default:
                    continue;
            }

            if (vertex.AssociatedStatement is not (OutputStatementNode or SwitchStatementNode))
            {
                continue;
            }

            writer.WriteLine($"case {index}:");

            writer.Indent++;
            writer.Write($"return ");
            GenerateExpression(writer, outputExpression);
            writer.WriteLine(";");
            writer.Indent--;
        }

        writer.Indent -= 2;

        writer.WriteManyLines(
            """
                    case -1:
                        return default;
                }

                throw new global::System.InvalidOperationException("Invalid state");
            }
            """);
    }

    private void GenerateGetOptionsMethod(IndentedTextWriter writer)
    {
        writer.Write("private global::System.Collections.Immutable.ImmutableArray<");
        GenerateType(writer, settings.OptionType);
        writer.WriteLine("> GetOptions()");

        writer.WriteManyLines(
              $$"""
              {
                  switch (state)
                  {
              """);

        writer.Indent += 2;

        foreach ((int index, FlowVertex vertex) in flowGraph.Vertices)
        {
            if (vertex.AssociatedStatement is SwitchStatementNode switchStatement)
            {
                writer.WriteLine($"case {index}:");

                writer.Indent++;
                writer.Write("return global::System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { ");

                foreach (SwitchOptionNode option in switchStatement.Options)
                {
                    GenerateExpression(writer, option.Expression);
                    writer.Write(", ");
                }

                writer.WriteLine("});");
                writer.Indent--;
            }
        }

        writer.Indent--;

        writer.WriteLine('}');
        writer.WriteLine();

        writer.Write("return global::System.Collections.Immutable.ImmutableArray<");
        GenerateType(writer, settings.OptionType);
        writer.WriteLine(">.Empty;");

        writer.Indent--;
        writer.WriteLine('}');
    }

    private void GenerateTypes(IndentedTextWriter writer)
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
        writer.WriteManyLines(
                        $$"""
                        public readonly struct @{{record.Name}} : global::System.IEquatable<@{{record.Name}}>
                        {
                        """);
        writer.Indent++;

        writer.Write($"internal @{record.Name}(");

        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            GenerateType(writer, property.Type);
            writer.Write($" @{property.Name}, ");
        }

        GenerateType(writer, record.Properties[^1].Type);
        writer.WriteLine($" @{record.Properties[^1].Name})");
        writer.WriteLine('{');

        writer.Indent++;

        foreach (PropertySymbol property in record.Properties)
        {
            writer.WriteLine($"this.@{property.Name} = @{property.Name};");
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        foreach (PropertySymbol property in record.Properties.Take(record.Properties.Length - 1))
        {
            GenerateType(writer, property.Type);
            writer.WriteLine($" @{property.Name} {{ get; }}");
            writer.WriteLine();
        }

        GenerateType(writer, record.Properties[^1].Type);
        writer.WriteLine($" @{record.Properties[^1].Name} {{ get; }}");
        writer.WriteLine();

        // union Equals()
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

        // object Equals()
        writer.WriteLine("public override bool Equals(object? other)");
        writer.WriteLine('{');
        writer.Indent++;
        writer.Write("return other is @");
        writer.Write(record.Name);
        writer.WriteLine(" record && Equals(record);");
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        // GetHashCode()
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

        writer.Indent--;
        writer.WriteLine('}');
    }

    private void GenerateUnionDeclaration(IndentedTextWriter writer, UnionTypeSymbol union)
    {
        void GenerateSubtypeName(TypeSymbol subtype, bool includeAt = true)
        {
            if (includeAt)
            {
                writer.Write('@');
            }

            writer.Write(subtype.Name);
        }

        void GenerateUnionInterfaceName()
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

        writer.Write("public readonly struct @");
        writer.Write(union.Name);
        writer.Write(" : global::System.IEquatable<@");
        writer.Write(union.Name);
        writer.Write('>');

        if (union.Subtypes.Length is >= 2 and <= 10)
        {
            // the IUnion interface only exists for those
            writer.Write(", ");
            GenerateUnionInterfaceName();
        }

        writer.WriteLine();
        writer.WriteLine('{');

        writer.Indent++;

        // constructors
        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write($"internal @{union.Name}(");
            GenerateType(writer, subtype);
            writer.WriteLine(" value)");
            writer.WriteLine('{');
            writer.Indent++;
            writer.Write("this.");
            GenerateSubtypeName(subtype);
            writer.WriteLine(" = value;");
            writer.Indent--;
            writer.WriteLine('}');
            writer.WriteLine();
        }

        // properties
        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write("public ");
            GenerateType(writer, subtype);
            writer.Write(' ');
            GenerateSubtypeName(subtype); // the property gets the same name as the type
            writer.WriteLine(" { get; }");
            writer.WriteLine();
        }

        writer.Write("public ");
        writer.Write(union.Name);
        writer.WriteLine("Discriminator Discriminator { get; }");
        writer.WriteLine();

        // AsObject()
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
            GenerateSubtypeName(subtype);
            writer.WriteLine(':');
            writer.Indent++;
            writer.Write("return this.");
            GenerateSubtypeName(subtype);
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

        // Run()
        writer.Write("public void Run(");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            writer.Write("global::System.Action<");
            GenerateType(writer, subtype);
            writer.Write("> action");
            GenerateSubtypeName(subtype, includeAt: false);
            writer.Write(", ");
        }

        writer.Write("global::System.Action<");
        GenerateType(writer, union.Subtypes[^1]);
        writer.Write("> action");
        GenerateSubtypeName(union.Subtypes[^1], includeAt: false);
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
            GenerateSubtypeName(subtype);
            writer.WriteLine(':');
            writer.Indent++;
            writer.Write("action");
            GenerateSubtypeName(subtype, includeAt: false);
            writer.Write("(this.");
            GenerateSubtypeName(subtype);
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

        // Evaluate()
        writer.Write("public T Evaluate<T>(");

        foreach (TypeSymbol subtype in union.Subtypes.Take(union.Subtypes.Length - 1))
        {
            writer.Write("global::System.Func<");
            GenerateType(writer, subtype);
            writer.Write(", T> function");
            GenerateSubtypeName(subtype, includeAt: false);
            writer.Write(", ");
        }

        writer.Write("global::System.Func<");
        GenerateType(writer, union.Subtypes[^1]);
        writer.Write(", T> function");
        GenerateSubtypeName(union.Subtypes[^1], includeAt: false);
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
            GenerateSubtypeName(subtype);
            writer.WriteLine(':');
            writer.Indent++;
            writer.Write("return function");
            GenerateSubtypeName(subtype, includeAt: false);
            writer.Write("(this.");
            GenerateSubtypeName(subtype);
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

        // union Equals()
        writer.Write("public bool Equals(@");
        writer.Write(union.Name);
        writer.WriteLine(" other)");
        writer.WriteLine('{');
        writer.Indent++;
        writer.Write("return Discriminator == other.Discriminator");

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            writer.Write(" && this.");
            GenerateSubtypeName(subtype);
            writer.Write(" == other.");
            GenerateSubtypeName(subtype);
        }

        writer.WriteLine(';');
        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        // object Equals()
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
            GenerateSubtypeName(subtype);
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

        // discriminator enum
        writer.Write("public enum ");
        writer.Write(union.Name);
        writer.WriteLine("Discriminator");
        writer.WriteLine('{');
        writer.Indent++;

        foreach (TypeSymbol subtype in union.Subtypes)
        {
            GenerateSubtypeName(subtype);
            writer.WriteLine(',');
        }

        writer.Indent--;
        writer.WriteLine('}');
        writer.WriteLine();

        // explicit interface implementation
        if (union.Subtypes.Length is >= 2 and <= 10)
        {
            for (int i = 0; i < union.Subtypes.Length; i++)
            {
                GenerateType(writer, union.Subtypes[i]);
                writer.Write(' ');
                GenerateUnionInterfaceName();
                writer.Write(".Value");
                writer.WriteLine(i);
                writer.WriteLine('{');
                writer.Indent++;
                writer.WriteLine("get");
                writer.WriteLine('{');
                writer.Indent++;
                writer.Write("return this.");
                GenerateSubtypeName(union.Subtypes[i]);
                writer.WriteLine(';');
                writer.Indent--;
                writer.WriteLine('}');
                writer.Indent--;
                writer.WriteLine('}');

                writer.WriteLine();
            }

            writer.Write("int ");
            GenerateUnionInterfaceName();
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

    private static void GenerateExpression(IndentedTextWriter writer, ExpressionNode expression)
    {
        TypedExpressionNode? typedExpression = expression as TypedExpressionNode;
        Debug.Assert(typedExpression is not null);

        if (typedExpression.SourceType != typedExpression.TargetType)
        {
            Debug.Assert(typedExpression.TargetType is UnionTypeSymbol);

            writer.Write("new @");
            writer.Write(typedExpression.TargetType.Name);
            writer.Write('(');
            GenerateExpression(writer, typedExpression with
            {
                TargetType = typedExpression.SourceType,
            });
            writer.Write(')');
            return;
        }

        switch (typedExpression.Expression)
        {
            case IntegerLiteralExpressionNode { Value: int intValue }:
                writer.Write(intValue);
                return;
            case StringLiteralExpressionNode { StringLiteral: string stringValue }:
                writer.Write(SymbolDisplay.FormatLiteral(stringValue, quote: true));
                return;
            case BoundRecordCreationExpressionNode recordCreation:
                writer.Write("new @");
                writer.Write(recordCreation.Record.Name);
                writer.Write('(');
                foreach (BoundArgumentNode argument in recordCreation.BoundArguments.Take(recordCreation.BoundArguments.Length - 1))
                {
                    GenerateExpression(writer, argument.Expression);
                    writer.Write(", ");
                }
                GenerateExpression(writer, recordCreation.BoundArguments[^1].Expression);
                writer.Write(')');
                return;
        }

        Debug.Assert(false);
    }

    private void GenerateType(IndentedTextWriter writer, TypeSymbol type)
    {
        switch (type)
        {
            case BuiltinTypeSymbol { Type: BuiltinType.Int }:
                writer.Write("int");
                return;
            case BuiltinTypeSymbol { Type: BuiltinType.String }:
                writer.Write("string?");
                return;
            default:
                writer.Write('@');
                writer.Write(settings.ClassName);
                writer.Write(".@");
                writer.Write(type.Name);
                return;
        }
    }

    private static string GetOutcomeFieldName(OutcomeSymbol outcome) => outcome.Index >= 0 ? $"outcome{outcome.Index}" : $"outcome_{-outcome.Index}";
}

#if EXAMPLE_STORY
public sealed class Story
{
    public readonly record struct OutputType(string Text);
    public readonly record struct SwitchType(ImmutableArray<OptionType> Options);
    public readonly record struct OptionType(string Text);

    public enum StoryStateKind
    {
        None = 0,
        Linear,
        Switch,
    }

    public Story()
    {
        state = 37191;
        StateKind = StoryStateKind.Linear;
    }

    private int state;

    int? somethingHappened = 1;

    public OutputType? Output { get; private set; }

    public SwitchType? SwitchOutput { get; private set; }

    public StoryStateKind StateKind { get; private set; }

    public bool TryContinue()
    {
        if (StateKind != StoryStateKind.Linear)
        {
            return false;
        }

        state = StateTransition(0);

        return true;
    }

    public bool TryContinueWithOption(int option)
    {
        if (StateKind != StoryStateKind.Switch)
        {
            return false;
        }

        state = StateTransition(option);

        return true;
    }

    private int StateTransition(int option)
    {
        while (true)
        {
            switch (state, option)
            {
                case (1249, _):
                    return 7292;
                case (618, 0):
                    return 1829;
                case (618, 1):
                    return 181010;
                case (1739, _):
                    state = somethingHappened == 0 ? 1910 : 19201;
                    continue;
                default: throw new System.InvalidOperationException();
            }
        }
    }

    private OutputType? GetOutput() => throw new System.NotImplementedException();

    private SwitchType? GetSwitchOutput() => throw new System.NotImplementedException();
}
#endif
