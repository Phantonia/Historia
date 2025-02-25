#nullable enable
internal struct Fields
{
    public long state;
    public int outcome19;

}

public sealed class HistoriaStoryStateMachine : global::Phantonia.Historia.IStoryStateMachine<int, int>
{
    public HistoriaStoryStateMachine()
    {
        fields.state = -2;
        options = new int[2];
    }

    private int optionsCount;
    private readonly int[] options;
    private Fields fields;

    public bool NotStartedStory { get; private set; } = true;

    public bool FinishedStory { get; private set; } = false;

    public bool CanContinueWithoutOption { get; private set; } = true;

    public global::Phantonia.Historia.ReadOnlyList<int> Options
    {
        get
        {
            return new global::Phantonia.Historia.ReadOnlyList<int>(options, 0, optionsCount);
        }
    }

    public int Output { get; private set; }

    public bool TryContinue()
    {
        if (!CanContinueWithoutOption)
        {
            return false;
        }

        Heart.StateTransition(ref fields, -1, out bool canContinueWithoutOption);
        CanContinueWithoutOption = canContinueWithoutOption;
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

        Heart.StateTransition(ref fields, option, out bool canContinueWithoutOption);
        CanContinueWithoutOption = canContinueWithoutOption;
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
        int[] optionsCopy = new int[options.Length];
        global::System.Array.Copy(options, optionsCopy, options.Length);
        return new HistoriaStorySnapshot(fields, Output, optionsCopy, optionsCount, CanContinueWithoutOption);
    }

    public void RestoreSnapshot(HistoriaStorySnapshot snapshot)
    {
        fields = snapshot.fields;
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

    global::System.Collections.Generic.IReadOnlyList<int> global::Phantonia.Historia.IStoryStateMachine<int, int>.Options
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
            return new global::Phantonia.Historia.ObjectReadOnlyList<int>(Options);
        }
    }
    global::Phantonia.Historia.IStorySnapshot global::Phantonia.Historia.IStoryStateMachine.CreateSnapshot()
    {
        return CreateSnapshot();
    }

    global::Phantonia.Historia.IStorySnapshot<int, int> global::Phantonia.Historia.IStoryStateMachine<int, int>.CreateSnapshot()
    {
        return CreateSnapshot();
    }
}

public sealed class HistoriaStorySnapshot : global::Phantonia.Historia.IStorySnapshot<int, int>
{

    internal HistoriaStorySnapshot(Fields fields, int output, int[] options, int optionsCount, bool canContinueWithoutOption)
    {
        this.fields = fields;
        Output = output;
        this.options = options;
        this.optionsCount = optionsCount;
        NotStartedStory = fields.state == -2;
        FinishedStory = fields.state == -1;
        CanContinueWithoutOption = canContinueWithoutOption;
    }

    private readonly int optionsCount;
    private readonly int[] options;
    internal readonly Fields fields;

    public bool NotStartedStory { get; } = true;

    public bool FinishedStory { get; } = false;

    public bool CanContinueWithoutOption { get; } = true;

    public global::Phantonia.Historia.ReadOnlyList<int> Options
    {
        get
        {
            return new global::Phantonia.Historia.ReadOnlyList<int>(options, 0, optionsCount);
        }
    }

    public int Output { get; }

    public HistoriaStorySnapshot? TryContinue()
    {
        if (!CanContinueWithoutOption)
        {
            return null;
        }

        Fields fieldsCopy = fields;
        Heart.StateTransition(ref fieldsCopy, -1, out bool canContinueWithoutOption);
        int output = Heart.GetOutput(ref fieldsCopy);
        int[] optionsCopy = new int[options.Length];
        int optionsCountCopy = optionsCount;
        Heart.GetOptions(ref fieldsCopy, optionsCopy, ref optionsCountCopy);
        return new HistoriaStorySnapshot(fieldsCopy, output, optionsCopy, optionsCountCopy, canContinueWithoutOption);
    }

    public HistoriaStorySnapshot? TryContinueWithOption(int option)
    {
        if (FinishedStory || option < 0 || option >= Options.Count)
        {
            return null;
        }
        Fields fieldsCopy = fields;
        Heart.StateTransition(ref fieldsCopy, option, out bool canContinueWithoutOption);
        int output = Heart.GetOutput(ref fieldsCopy);
        int[] optionsCopy = new int[options.Length];
        int optionsCountCopy = optionsCount;
        Heart.GetOptions(ref fieldsCopy, optionsCopy, ref optionsCountCopy);
        return new HistoriaStorySnapshot(fieldsCopy, output, optionsCopy, optionsCountCopy, canContinueWithoutOption);
    }

    object global::Phantonia.Historia.IStorySnapshot.Output
    {
        get
        {
            return Output;
        }
    }

    global::System.Collections.Generic.IReadOnlyList<int> global::Phantonia.Historia.IStorySnapshot<int, int>.Options
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
            return new global::Phantonia.Historia.ObjectReadOnlyList<int>(Options);
        }
    }

    global::Phantonia.Historia.IStorySnapshot<int, int>? global::Phantonia.Historia.IStorySnapshot<int, int>.TryContinue()
    {
        return TryContinue();
    }

    global::Phantonia.Historia.IStorySnapshot<int, int>? global::Phantonia.Historia.IStorySnapshot<int, int>.TryContinueWithOption(int option)
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

    public static void StateTransition(ref Fields fields, int option, out bool canContinueWithoutOption)
    {
        while (true)
        {
            switch (fields.state)
            {
                case -2:
                    fields.state = 43;
                    canContinueWithoutOption = true;
                    return;
                case 43:
                    switch (option)
                    {
                        case -1:
                            fields.state = 68;
                            canContinueWithoutOption = true;
                            return;
                        case 0:
                            fields.state = 157;
                            canContinueWithoutOption = true;
                            return;
                        case 1:
                            fields.state = 222;
                            canContinueWithoutOption = true;
                            return;
                    }

                    break;
                case 68:
                    switch (option)
                    {
                        case -1:
                            fields.state = 87;
                            continue;
                        case 0:
                            fields.state = 157;
                            canContinueWithoutOption = true;
                            return;
                        case 1:
                            fields.state = 222;
                            canContinueWithoutOption = true;
                            return;
                    }

                    break;
                case 87:
                    fields.outcome19 = 0;
                    fields.state = 103;
                    canContinueWithoutOption = false;
                    return;
                case 103:
                    switch (option)
                    {
                        case 0:
                            fields.state = -1;
                            canContinueWithoutOption = false;
                            return;
                        case 1:
                            fields.state = 157;
                            canContinueWithoutOption = true;
                            return;
                        case 2:
                            fields.state = 222;
                            canContinueWithoutOption = true;
                            return;
                    }

                    break;
                case 157:
                    fields.state = -1;
                    canContinueWithoutOption = false;
                    return;
                case 222:
                    fields.state = -1;
                    canContinueWithoutOption = false;
                    return;
            }

            throw new global::System.InvalidOperationException("Fatal internal error: Invalid state (StateTransition)");
        }
    }

    public static int GetOutput(ref Fields fields)
    {
        switch (fields.state)
        {
            case 43:
                return 1;
            case 68:
                return 2;
            case 103:
                return 3;
            case 157:
                return 5;
            case 222:
                return 7;
            case -1:
                return default;
        }

        throw new global::System.InvalidOperationException("Fatal internal error: Invalid state (GetOutput)");
    }

    public static void GetOptions(ref Fields fields, int[] options, ref int optionsCount)
    {
        switch (fields.state)
        {
            case 43:
                global::System.Array.Clear(options);
                options[0] = 4;
                options[1] = 6;
                optionsCount = 2;
                return;
        }

        optionsCount = 0;
    }
}

public static class HistoriaStoryGraph
{
    public static global::Phantonia.Historia.StoryGraph<int, int> CreateStoryGraph()
    {
        global::System.Collections.Generic.Dictionary<long, global::Phantonia.Historia.StoryVertex<int, int>> vertices = new global::System.Collections.Generic.Dictionary<long, global::Phantonia.Historia.StoryVertex<int, int>>(5);

        {
            int[] options = { 4, 6, };

            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(68, 43, false),
                new global::Phantonia.Historia.StoryEdge(157, 43, false),
                new global::Phantonia.Historia.StoryEdge(222, 43, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(43, -2, false),
            };

            vertices[43] = new global::Phantonia.Historia.StoryVertex<int, int>(43, 1, new global::Phantonia.Historia.ReadOnlyList<int>(options), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(157, 68, false),
                new global::Phantonia.Historia.StoryEdge(222, 68, false),
                new global::Phantonia.Historia.StoryEdge(103, 68, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(68, 43, false),
            };

            vertices[68] = new global::Phantonia.Historia.StoryVertex<int, int>(68, 2, new global::Phantonia.Historia.ReadOnlyList<int>(global::System.Array.Empty<int>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(-1, 103, false),
                new global::Phantonia.Historia.StoryEdge(157, 103, false),
                new global::Phantonia.Historia.StoryEdge(222, 103, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(103, 68, false),
            };

            vertices[103] = new global::Phantonia.Historia.StoryVertex<int, int>(103, 3, new global::Phantonia.Historia.ReadOnlyList<int>(global::System.Array.Empty<int>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(-1, 157, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(157, 43, false),
                new global::Phantonia.Historia.StoryEdge(157, 68, false),
                new global::Phantonia.Historia.StoryEdge(157, 103, false),
            };

            vertices[157] = new global::Phantonia.Historia.StoryVertex<int, int>(157, 5, new global::Phantonia.Historia.ReadOnlyList<int>(global::System.Array.Empty<int>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(-1, 222, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(222, 43, false),
                new global::Phantonia.Historia.StoryEdge(222, 68, false),
                new global::Phantonia.Historia.StoryEdge(222, 103, false),
            };

            vertices[222] = new global::Phantonia.Historia.StoryVertex<int, int>(222, 7, new global::Phantonia.Historia.ReadOnlyList<int>(global::System.Array.Empty<int>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        global::Phantonia.Historia.StoryEdge[] startEdges = new global::Phantonia.Historia.StoryEdge[1];
        startEdges[0] = new global::Phantonia.Historia.StoryEdge(43, -2, false);
        return new global::Phantonia.Historia.StoryGraph<int, int>(vertices, startEdges);
    }
}

