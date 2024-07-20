using Phantonia.Historia.Language.FlowAnalysis;
using System.CodeDom.Compiler;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class HeartEmitter
{
    public HeartEmitter(FlowGraph flowGraph, Settings settings, IndentedTextWriter writer)
    {
        this.flowGraph = flowGraph;
        this.settings = settings;
        this.writer = writer;
    }

    private readonly FlowGraph flowGraph;
    private readonly Settings settings;
    private readonly IndentedTextWriter writer;

    public void GenerateHeartClass()
    {
        writer.WriteLine("internal static class Heart");
        writer.BeginBlock();

        StateTransitionEmitter stateTransitionEmitter = new(flowGraph, settings, writer);
        stateTransitionEmitter.GenerateStateTransitionMethod();

        writer.WriteLine();

        OutputEmitter outputEmitter = new(flowGraph, settings, writer);
        outputEmitter.GenerateOutputMethods();

        writer.EndBlock();
    }
}
