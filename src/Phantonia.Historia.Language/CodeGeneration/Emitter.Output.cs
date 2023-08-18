using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.CodeDom.Compiler;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed partial class Emitter
{
    private void GenerateGetOutputMethod()
    {
        writer.Write("private ");
        GenerateType(settings.OutputType);
        writer.WriteLine(" GetOutput()");

        writer.WriteLine('{');
        writer.Indent++;

        writer.WriteLine("switch (state)");
        writer.WriteLine('{');

        writer.Indent++;

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

            writer.Write("case ");
            writer.Write(index);
            writer.WriteLine(':');

            writer.Indent++;
            writer.Write($"return ");
            GenerateExpression(outputExpression);
            writer.WriteLine(";");
            writer.Indent--;
        }

        writer.Indent -= 2;

        writer.Write("case ");
        writer.Write(EndState);
        writer.WriteLine(":");

        writer.WriteManyLines(
            """
                        return default;
                }

                throw new global::System.InvalidOperationException("Invalid state");
            }
            """);
    }

    private void GenerateGetOptionsMethod()
    {
        writer.Write("private global::System.Collections.Immutable.ImmutableArray<");
        GenerateType(settings.OptionType);
        writer.WriteLine("> GetOptions()");
        writer.WriteLine('{');

        writer.Indent++;
        writer.WriteLine("switch (state)");
        writer.WriteLine('{');

        writer.Indent++;

        foreach ((int index, FlowVertex vertex) in flowGraph.Vertices)
        {
            if (vertex.AssociatedStatement is SwitchStatementNode switchStatement)
            {
                writer.Write("case ");
                writer.Write(index);
                writer.WriteLine(':');

                writer.Indent++;
                writer.Write("return global::System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { ");

                foreach (SwitchOptionNode option in switchStatement.Options)
                {
                    GenerateExpression(option.Expression);
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
        GenerateType(settings.OptionType);
        writer.WriteLine(">.Empty;");

        writer.Indent--;
        writer.WriteLine('}');
    }
}
