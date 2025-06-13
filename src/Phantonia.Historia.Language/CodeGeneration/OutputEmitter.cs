using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Expressions;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class OutputEmitter(FlowGraph flowGraph, Settings settings, IndentedTextWriter writer)
{
    public void GenerateOutputMethods()
    {
        GenerateGetOutputMethod();

        writer.WriteLine();

        GenerateGetOptionsMethod();
    }

    private void GenerateGetOutputMethod()
    {
        writer.Write("public static ");
        GeneralEmission.GenerateType(settings.OutputType, writer);
        writer.WriteLine(" GetOutput(ref Fields fields)");

        writer.BeginBlock();

        writer.WriteLine("switch (fields.state)");
        writer.BeginBlock();

        foreach ((uint index, FlowVertex vertex) in flowGraph.Vertices)
        {
            if (!vertex.IsStory)
            {
                continue;
            }

            ExpressionNode outputExpression;

            switch (vertex.AssociatedStatement)
            {
                case IOutputStatementNode outputStatement:
                    outputExpression = outputStatement.OutputExpression;
                    break;
                case FlowBranchingStatementNode { Original: IOutputStatementNode outputStatement }:
                    outputExpression = outputStatement.OutputExpression;
                    break;
                default:
                    continue;
            }

            writer.Write("case ");
            writer.Write(index);
            writer.WriteLine(':');

            writer.Indent++;
            writer.Write($"return ");
            GeneralEmission.GenerateExpression(outputExpression, writer);
            writer.WriteLine(";");
            writer.Indent--;
        }

        writer.Write("case ");
        writer.Write(FlowGraph.Source);
        writer.WriteLine(":");
        writer.Write("case ");
        writer.Write(FlowGraph.Sink);
        writer.WriteLine(':');
        writer.Indent++;

        writer.WriteLine("return default;");
        writer.Indent--;

        writer.EndBlock(); // switch
        writer.WriteLine();

        writer.WriteLine("throw new global::System.InvalidOperationException(\"Fatal internal error: Invalid state (GetOutput)\");");

        writer.EndBlock(); // GetOutput method
    }

    private void GenerateGetOptionsMethod()
    {
        writer.Write("public static void GetOptions(ref Fields fields, ");
        GeneralEmission.GenerateType(settings.OptionType, writer);
        writer.WriteLine("[] options, ref int optionsCount)");

        writer.BeginBlock();

        if (flowGraph.Vertices.Values.Any(v => v.IsStory && v.AssociatedStatement is FlowBranchingStatementNode { Original: SwitchStatementNode or LoopSwitchStatementNode }))
        {
            writer.WriteLine("global::System.Array.Clear(options);");

            writer.WriteLine("switch (fields.state)");
            writer.BeginBlock();

            foreach ((uint index, FlowVertex vertex) in flowGraph.Vertices)
            {
                void GenerateOptions(IEnumerable<ExpressionNode> optionExpressions)
                {
                    writer.Write("case ");
                    writer.Write(index);
                    writer.WriteLine(':');

                    writer.Indent++;

                    foreach ((ExpressionNode optionExpression, int i) in optionExpressions.Select((o, i) => (o, i)))
                    {
                        writer.Write("options[");
                        writer.Write(i);
                        writer.Write("] = ");
                        GeneralEmission.GenerateExpression(optionExpression, writer);
                        writer.WriteLine(';');
                    }

                    writer.Write("optionsCount = ");
                    writer.Write(optionExpressions.Count());
                    writer.WriteLine(';');

                    writer.WriteLine("return;");

                    writer.Indent--;
                }

                if (vertex.AssociatedStatement is FlowBranchingStatementNode { Original: SwitchStatementNode switchStatement })
                {
                    GenerateOptions(switchStatement.Options.Select(o => o.Expression));
                }
                else if (vertex.AssociatedStatement is DynamicSwitchFlowBranchingStatementNode { OptionExpressions: ImmutableArray<ExpressionNode> optionExpressions })
                {
                    GenerateOptions(optionExpressions);
                }
                else if (vertex.AssociatedStatement is FlowBranchingStatementNode { Original: LoopSwitchStatementNode loopSwitchStatement })
                {
                    writer.Write("case ");
                    writer.Write(index);
                    writer.WriteLine(':');
                    writer.Indent++;

                    writer.BeginBlock();

                    writer.WriteLine("int i = 0;");

                    for (int i = 0; i < loopSwitchStatement.Options.Length; i++)
                    {
                        writer.WriteLine();

                        // if the condition ((ls & (1 << i)) == 0) is true,
                        // the corresponding option has not yet been selected,
                        // so we put it into the array
                        writer.Write("if ((fields.");
                        GeneralEmission.GenerateLoopSwitchFieldName(loopSwitchStatement, writer);
                        writer.Write(" & (1UL << ");
                        writer.Write(i);
                        writer.WriteLine(")) == 0)");

                        writer.BeginBlock();

                        writer.Write("options[i] = ");
                        GeneralEmission.GenerateExpression(loopSwitchStatement.Options[i].Expression, writer);
                        writer.WriteLine(';');

                        writer.WriteLine("i++;");

                        writer.EndBlock(); // if
                    }

                    writer.WriteLine();

                    // i is the next index, but at the end, that is just the length
                    writer.WriteLine("optionsCount = i;");

                    writer.WriteLine("return;");

                    writer.EndBlock(); // scope

                    writer.Indent--;
                }
            }

            writer.EndBlock(); // switch
            writer.WriteLine();
        }

        writer.WriteLine("optionsCount = 0;");

        writer.EndBlock(); // GetOptions emitter
    }
}
