#nullable enable
public interface ISomething
{
    void Do(string? x, int y);
    int What(string? x, global::Phantonia.Historia.ReadOnlyList<string?> options);
}


internal struct Fields
{
    public int state;
    public ISomething referenceDomething;
    public int outcome411;
}

public sealed class HistoriaStoryStateMachine : global::Phantonia.Historia.IStoryStateMachine<int, string?>
{
    public HistoriaStoryStateMachine(ISomething referenceDomething)
    {
        fields.state = -2;
        fields.referenceDomething = referenceDomething;
        options = global::System.Array.Empty<string?>();
    }

    private int optionsCount;
    private readonly string?[] options;
    private Fields fields;

    public bool NotStartedStory { get; private set; } = true;

    public bool FinishedStory { get; private set; } = false;

    public global::Phantonia.Historia.ReadOnlyList<string?> Options
    {
        get
        {
            return new global::Phantonia.Historia.ReadOnlyList<string?>(options, 0, optionsCount);
        }
    }

    public int Output { get; private set; }

    public ISomething ReferenceDomething
    {
        get
        {
            return fields.referenceDomething;
        }
        set
        {
            fields.referenceDomething = value;
        }
    }

    public bool TryContinue()
    {
        if (FinishedStory || Options.Count != 0)
        {
            return false;
        }

        Heart.StateTransition(ref fields, 0);
        Output = Heart.GetOutput(ref fields);
        Heart.GetOptions(ref fields, options, ref optionsCount);

        if (fields.state != -2)
        {
            NotStartedStory = false;
        }

        if (fields.state == -1)
        {
            FinishedStory = true;
        }

        return true;
    }

    public bool TryContinueWithOption(int option)
    {
        if (FinishedStory || option < 0 || option >= Options.Count)
        {
            return false;
        }

        Heart.StateTransition(ref fields, option);
        Output = Heart.GetOutput(ref fields);
        Heart.GetOptions(ref fields, options, ref optionsCount);

        if (fields.state != -2)
        {
            NotStartedStory = false;
        }

        if (fields.state == -1)
        {
            FinishedStory = true;
        }

        return true;
    }

    public HistoriaStorySnapshot CreateSnapshot()
    {
        string?[] optionsCopy = new string?[options.Length];
        global::System.Array.Copy(options, optionsCopy, options.Length);
        return new HistoriaStorySnapshot(fields, Output, optionsCopy, optionsCount);
    }

    public void RestoreSnapshot(HistoriaStorySnapshot snapshot)
    {
        fields = snapshot.fields;
        Output = Heart.GetOutput(ref fields);
        Heart.GetOptions(ref fields, options, ref optionsCount);
    }

    public void RestoreCheckpoint(HistoriaStoryCheckpoint checkpoint)
    {
        if (!checkpoint.IsReady())
        {
            throw new global::System.ArgumentException("Checkpoint is not ready, i.e. fully initialized");
        }

        fields.state = checkpoint.Index;

        Output = Heart.GetOutput(ref fields);
        Heart.GetOptions(ref fields, options, ref optionsCount);
    }

    object global::Phantonia.Historia.IStoryStateMachine.Output
    {
        get
        {
            return Output;
        }
    }

    global::System.Collections.Generic.IReadOnlyList<string?> global::Phantonia.Historia.IStoryStateMachine<int, string?>.Options
    {
        get
        {
            return Options;
        }
    }

    global::System.Collections.Generic.IReadOnlyList<object?> global::Phantonia.Historia.IStoryStateMachine.Options
    {
        get
        {
            return new global::Phantonia.Historia.ObjectReadOnlyList<string?>(Options);
        }
    }
    global::Phantonia.Historia.IStorySnapshot global::Phantonia.Historia.IStoryStateMachine.CreateSnapshot()
    {
        return CreateSnapshot();
    }

    global::Phantonia.Historia.IStorySnapshot<int, string?> global::Phantonia.Historia.IStoryStateMachine<int, string?>.CreateSnapshot()
    {
        return CreateSnapshot();
    }
}

public sealed class HistoriaStorySnapshot : global::Phantonia.Historia.IStorySnapshot<int, string?>
{
    public static HistoriaStorySnapshot FromCheckpoint(HistoriaStoryCheckpoint checkpoint, ISomething referenceDomething)
    {
        HistoriaStoryStateMachine stateMachine = new HistoriaStoryStateMachine(referenceDomething);
        stateMachine.RestoreCheckpoint(checkpoint);
        return stateMachine.CreateSnapshot();
    }

    internal HistoriaStorySnapshot(Fields fields, int output, string?[] options, int optionsCount)
    {
        this.fields = fields;
        Output = output;
        this.options = options;
        this.optionsCount = optionsCount;
        NotStartedStory = fields.state == -2;
        FinishedStory = fields.state == -1;
    }

    private readonly int optionsCount;
    private readonly string?[] options;
    internal readonly Fields fields;

    public bool NotStartedStory { get; } = true;

    public bool FinishedStory { get; } = false;

    public global::Phantonia.Historia.ReadOnlyList<string?> Options
    {
        get
        {
            return new global::Phantonia.Historia.ReadOnlyList<string?>(options, 0, optionsCount);
        }
    }

    public int Output { get; }

    public ISomething ReferenceDomething
    {
        get
        {
            return fields.referenceDomething;
        }
    }

    public HistoriaStorySnapshot? TryContinue()
    {
        if (FinishedStory || Options.Count != 0)
        {
            return null;
        }

        Fields fieldsCopy = fields;
        Heart.StateTransition(ref fieldsCopy, 0);
        int output = Heart.GetOutput(ref fieldsCopy);
        string?[] optionsCopy = new string?[options.Length];
        int optionsCountCopy = optionsCount;
        Heart.GetOptions(ref fieldsCopy, optionsCopy, ref optionsCountCopy);
        return new HistoriaStorySnapshot(fieldsCopy, output, optionsCopy, optionsCountCopy);
    }

    public HistoriaStorySnapshot? TryContinueWithOption(int option)
    {
        if (FinishedStory || option < 0 || option >= Options.Count)
        {
            return null;
        }
        Fields fieldsCopy = fields;
        Heart.StateTransition(ref fieldsCopy, option);
        int output = Heart.GetOutput(ref fieldsCopy);
        string?[] optionsCopy = new string?[options.Length];
        int optionsCountCopy = optionsCount;
        Heart.GetOptions(ref fieldsCopy, optionsCopy, ref optionsCountCopy);
        return new HistoriaStorySnapshot(fieldsCopy, output, optionsCopy, optionsCountCopy);
    }

    public HistoriaStorySnapshot SetReferenceDomething(ISomething newReference)
    {
        Fields fieldsCopy = fields;
        fieldsCopy.referenceDomething = newReference;
        return new HistoriaStorySnapshot(fieldsCopy, Output, options, optionsCount);
    }

    object global::Phantonia.Historia.IStorySnapshot.Output
    {
        get
        {
            return Output;
        }
    }

    global::System.Collections.Generic.IReadOnlyList<string?> global::Phantonia.Historia.IStorySnapshot<int, string?>.Options
    {
        get
        {
            return Options;
        }
    }

    global::System.Collections.Generic.IReadOnlyList<object?> global::Phantonia.Historia.IStorySnapshot.Options
    {
        get
        {
            return new global::Phantonia.Historia.ObjectReadOnlyList<string?>(Options);
        }
    }

    global::Phantonia.Historia.IStorySnapshot<int, string?>? global::Phantonia.Historia.IStorySnapshot<int, string?>.TryContinue()
    {
        return TryContinue();
    }

    global::Phantonia.Historia.IStorySnapshot<int, string?>? global::Phantonia.Historia.IStorySnapshot<int, string?>.TryContinueWithOption(int option)
    {
        return TryContinueWithOption(option);
    }

    global::Phantonia.Historia.IStorySnapshot? global::Phantonia.Historia.IStorySnapshot.TryContinueWithOption(int option)
    {
        return TryContinueWithOption(option);
    }
    global::Phantonia.Historia.IStorySnapshot? global::Phantonia.Historia.IStorySnapshot.TryContinue()
    {
        return TryContinue();
    }
}

internal static class Heart
{
    private static readonly global::System.Threading.ThreadLocal<string?[]> optionsPool = new global::System.Threading.ThreadLocal<string?[]>(() => new string?[2]);

    public static void StateTransition(ref Fields fields, int option)
    {
        while (true)
        {
            switch (fields.state)
            {
                case -2:
                    fields.state = 179;
                    return;
                case 179:
                    fields.state = 211;
                    continue;
                case 211:
                    fields.referenceDomething.Do("xyz", 4);
                    fields.state = 246;
                    continue;
                case 246:
                    {
                        string?[] options = optionsPool.Value!;
                        options[0] = "YES!!!";
                        options[1] = "no :(";

                        int next = fields.referenceDomething.What("the fuck", new global::Phantonia.Historia.ReadOnlyList<string?>(options, 0, 2));

                        switch (next)
                        {
                            case 0:
                                fields.state = 338;
                                return;
                            case 1:
                                fields.state = 441;
                                continue;
                            default:
                                throw new global::System.InvalidOperationException($"Expected an option between 0 and 1, instead got option {next}");
                        }
                    }
                case 338:
                    fields.state = -1;
                    return;
                case 441:
                    fields.outcome411 = 0;
                    fields.state = 461;
                    return;
                case 461:
                    fields.state = -1;
                    return;
            }

            throw new global::System.InvalidOperationException("Fatal internal error: Invalid state");
        }
    }

    public static int GetOutput(ref Fields fields)
    {
        switch (fields.state)
        {
            case 179:
                return 0;
            case 338:
                return 1;
            case 461:
                return 2;
            case -1:
                return default;
        }

        throw new global::System.InvalidOperationException("Invalid state");
    }

    public static void GetOptions(ref Fields fields, string?[] options, ref int optionsCount)
    {
        optionsCount = 0;
    }
}

public static class HistoriaStoryGraph
{
    public static global::Phantonia.Historia.StoryGraph<int, string?> CreateStoryGraph()
    {
        global::System.Collections.Generic.Dictionary<int, global::Phantonia.Historia.StoryVertex<int, string?>> vertices = new global::System.Collections.Generic.Dictionary<int, global::Phantonia.Historia.StoryVertex<int, string?>>(3);

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(338, 179, false),
                new global::Phantonia.Historia.StoryEdge(461, 179, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(179, -2, false),
            };

            vertices[179] = new global::Phantonia.Historia.StoryVertex<int, string?>(179, 0, new global::Phantonia.Historia.ReadOnlyList<string?>(global::System.Array.Empty<string?>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(-1, 338, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(338, 179, false),
            };

            vertices[338] = new global::Phantonia.Historia.StoryVertex<int, string?>(338, 1, new global::Phantonia.Historia.ReadOnlyList<string?>(global::System.Array.Empty<string?>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(-1, 461, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(461, 179, false),
            };

            vertices[461] = new global::Phantonia.Historia.StoryVertex<int, string?>(461, 2, new global::Phantonia.Historia.ReadOnlyList<string?>(global::System.Array.Empty<string?>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        global::Phantonia.Historia.StoryEdge[] startEdges = new global::Phantonia.Historia.StoryEdge[1];
        startEdges[0] = new global::Phantonia.Historia.StoryEdge(179, -2, false);
        return new global::Phantonia.Historia.StoryGraph<int, string?>(vertices, startEdges);
    }
}
public struct HistoriaStoryCheckpoint
{
    private HistoriaStoryCheckpoint(int index)
    {
        Index = index;
    }

    public int Index { get; }
    public static HistoriaStoryCheckpoint GetForIndex(int index)
    {
        HistoriaStoryCheckpoint instance = new HistoriaStoryCheckpoint(index);

        switch (index)
        {
            case 179:
                break;
            default:
                throw new global::System.ArgumentException("index " + index + " is not a checkpoint");
        }

        return instance;
    }
    public bool IsReady()
    {
        return true;
    }
}
