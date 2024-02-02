#nullable enable
public sealed class @HistoriaStory : global::Phantonia.Historia.IStory<string?, string?>
{
    public @HistoriaStory()
    {
        state = -2;
        options = new string?[3];
    }
    
    private int state;
    private int optionsCount;
    private string?[] options;
    private int outcome79;
    
    public bool NotStartedStory { get; private set; } = true;
    
    public bool FinishedStory { get; private set; } = false;
    
    public global::Phantonia.Historia.ReadOnlyList<string?> Options
    {
        get
        {
            return new global::Phantonia.Historia.ReadOnlyList<string?>(options, 0, optionsCount);
        }
    }
    
    public string? Output { get; private set; }
    
    public bool TryContinue()
    {
        if (FinishedStory || Options.Count != 0)
        {
            return false;
        }
    
        StateTransition(0);
        Output = GetOutput();
        GetOptions();
    
        if (state != -2)
        {
            NotStartedStory = false;
        }
    
        if (state == -1)
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
    
        StateTransition(option);
        Output = GetOutput();
        GetOptions();
    
        if (state != -2)
        {
            NotStartedStory = false;
        }
    
        if (state == -1)
        {
            FinishedStory = true;
        }
    
        return true;
    }
    
    private void StateTransition(int option)
    {
        while (true)
        {
            switch (state)
            {
                case -2:
                    state = 117;
                    return;
                case 117:
                    state = 223;
                    return;
                case 223:
                    switch (option)
                    {
                        case 0:
                            state = 307;
                            return;
                        case 1:
                            state = 470;
                            return;
                        case 2:
                            state = 1370;
                            return;
                    }
                    
                    break;
                case 307:
                    state = 377;
                    continue;
                case 377:
                    outcome79 = 0;
                    state = 1564;
                    return;
                case 470:
                    state = 578;
                    return;
                case 578:
                    state = 653;
                    return;
                case 653:
                    state = 690;
                    return;
                case 690:
                    state = 739;
                    return;
                case 739:
                    switch (option)
                    {
                        case 0:
                            state = 870;
                            return;
                        case 1:
                            state = 1080;
                            return;
                    }
                    
                    break;
                case 870:
                    state = 933;
                    continue;
                case 933:
                    outcome79 = 0;
                    state = 1564;
                    return;
                case 1080:
                    state = 1167;
                    return;
                case 1167:
                    state = 1222;
                    continue;
                case 1222:
                    outcome79 = 1;
                    state = 1564;
                    return;
                case 1370:
                    state = 1466;
                    return;
                case 1466:
                    state = 1513;
                    continue;
                case 1513:
                    outcome79 = 1;
                    state = 1564;
                    return;
                case 1564:
                    state = 1615;
                    continue;
                case 1615:
                    switch (outcome79)
                    {
                        case 0:
                            state = 1687;
                            return;
                        case 1:
                            state = 1805;
                            return;
                    }
                    
                    throw new global::System.InvalidOperationException("Fatal internal error: Invalid outcome");
                case 1687:
                    state = -1;
                    return;
                case 1805:
                    state = -1;
                    return;
            }
    
            throw new global::System.InvalidOperationException("Fatal internal error: Invalid state");
        }
    }
    
    private string? GetOutput()
    {
        switch (state)
        {
            case 117:
                return "Jonathan: Come on, Alice. Press the button. It's finally time to end this fucking world.";
            case 223:
                return "What do you do?";
            case 307:
                return "Alice: You are right, Jonathan. Let's do this!";
            case 470:
                return "Alice: I'm really not sure this is the right thing to do. What about all the people?";
            case 578:
                return "Jonathan: Don't pretend you care about all of them!";
            case 653:
                return "Alice: Hmm...";
            case 690:
                return "Jonathan: Press it now!";
            case 739:
                return "What do you do now?";
            case 870:
                return "Alice: Okay, here goes nothing.";
            case 1080:
                return "Alice: No, I won't. No one will ever press this button.";
            case 1167:
                return "Jonathan: How dare you!";
            case 1370:
                return "Alice: I can't. And actually, I'll make it so no one will ever press it.";
            case 1466:
                return "Jonathan: How dare you!";
            case 1564:
                return "Time stands still for a second...";
            case 1687:
                return "And then the explosion wipes out all life on earth.";
            case 1805:
                return "And then nothing happens.";
            case -1:
                return default;
        }
        
        throw new global::System.InvalidOperationException("Invalid state");
    }
    
    private void GetOptions()
    {
        switch (state)
        {
            case 223:
                global::System.Array.Clear(options);
                options[0] = "Do it";
                options[1] = "Should I really?";
                options[2] = "Destroy the button without pressing it";
                optionsCount = 3;
                return;
            case 739:
                global::System.Array.Clear(options);
                options[0] = "Press the button";
                options[1] = "Destroy the button without pressing it";
                optionsCount = 2;
                return;
        }
        
        optionsCount = 0;
    }
    
    object global::Phantonia.Historia.IStory.Output
    {
        get
        {
            return Output;
        }
    }
    
    global::System.Collections.Generic.IReadOnlyList<string?> global::Phantonia.Historia.IStory<string?, string?>.Options
    {
        get
        {
            return Options;
        }
    }
    
    global::System.Collections.Generic.IReadOnlyList<object?> global::Phantonia.Historia.IStory.Options
    {
        get
        {
            return new global::Phantonia.Historia.ObjectReadOnlyList<string?>(Options);
        }
    }
}

