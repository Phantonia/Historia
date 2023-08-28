using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.Linq;

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

        writer.Write("case ");
        writer.Write(EndState);
        writer.WriteLine(":");
        writer.Indent++;

        writer.WriteLine("return default;");
        writer.Indent -= 2;

        writer.WriteLine('}');
        writer.WriteLine();

        writer.WriteLine("throw new global::System.InvalidOperationException(\"Invalid state\");");

        writer.Indent--;
        writer.WriteLine('}');
    }

    private void GenerateGetOptionsMethod()
    {
        writer.WriteLine("private void GetOptions()");
        
        writer.WriteLine('{');

        writer.Indent++;

        writer.WriteLine("global::System.Array.Clear(options);");
        writer.WriteLine();

        if (flowGraph.Vertices.Values.Any(v => v.AssociatedStatement is SwitchStatementNode))
        {
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

                    writer.WriteLine("global::System.Array.Clear(options);");

                    for (int i = 0; i < switchStatement.Options.Length; i++)
                    {
                        writer.Write("options[");
                        writer.Write(i);
                        writer.Write("] = ");
                        GenerateExpression(switchStatement.Options[i].Expression);
                        writer.WriteLine(';');
                    }

                    writer.Write("optionsCount = ");
                    writer.Write(switchStatement.Options.Length);
                    writer.WriteLine(';');

                    writer.WriteLine("return;");

                    writer.Indent--;
                }
            }

            writer.Indent--;

            writer.WriteLine('}');
            writer.WriteLine();
        }

        writer.WriteLine("optionsCount = 0;");

        writer.Indent--;
        writer.WriteLine('}');
    }
}
