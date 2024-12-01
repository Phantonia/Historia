#nullable enable
public enum @Character
{
    Lucy,
    Freedom,
    Caylee,
    Mart,
    Mike,
}

public readonly struct @StageDirection : global::System.IEquatable<@StageDirection>
{
    internal @StageDirection(string? @Text)
    {
        this.@Text = @Text;
    }

    public string? @Text { get; }

    public bool Equals(@StageDirection other)
    {
        return @Text == other.@Text;
    }

    public override bool Equals(object? other)
    {
        return other is @StageDirection record && Equals(record);
    }

    public override int GetHashCode()
    {
        global::System.HashCode hashcode = default;
        hashcode.Add(@Text);
        return hashcode.ToHashCode();
    }

    public static bool operator ==(@StageDirection x, @StageDirection y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(@StageDirection x, @StageDirection y)
    {
        return !x.Equals(y);
    }
}

public readonly struct @Line : global::System.IEquatable<@Line>
{
    internal @Line(@Character @Character, string? @StageDirection, string? @Text)
    {
        this.@Character = @Character; this.@StageDirection = @StageDirection; this.@Text = @Text;
    }

    public @Character @Character { get; }

    public string? @StageDirection { get; }

    public string? @Text { get; }

    public bool Equals(@Line other)
    {
        return @Character == other.@Character && @StageDirection == other.@StageDirection && @Text == other.@Text;
    }

    public override bool Equals(object? other)
    {
        return other is @Line record && Equals(record);
    }

    public override int GetHashCode()
    {
        global::System.HashCode hashcode = default;
        hashcode.Add(@Character);
        hashcode.Add(@StageDirection);
        hashcode.Add(@Text);
        return hashcode.ToHashCode();
    }

    public static bool operator ==(@Line x, @Line y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(@Line x, @Line y)
    {
        return !x.Equals(y);
    }
}

public enum @Severity
{
    Minor,
    Major,
}

public readonly struct @Choice : global::System.IEquatable<@Choice>
{
    internal @Choice(@Character @Character, @Severity @Severity)
    {
        this.@Character = @Character; this.@Severity = @Severity;
    }

    public @Character @Character { get; }

    public @Severity @Severity { get; }

    public bool Equals(@Choice other)
    {
        return @Character == other.@Character && @Severity == other.@Severity;
    }

    public override bool Equals(object? other)
    {
        return other is @Choice record && Equals(record);
    }

    public override int GetHashCode()
    {
        global::System.HashCode hashcode = default;
        hashcode.Add(@Character);
        hashcode.Add(@Severity);
        return hashcode.ToHashCode();
    }

    public static bool operator ==(@Choice x, @Choice y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(@Choice x, @Choice y)
    {
        return !x.Equals(y);
    }
}

public readonly struct @Output : global::System.IEquatable<@Output>, global::Phantonia.Historia.IUnion<@Line, @StageDirection, @Choice>
{
    internal @Output(@Line value)
    {
        this.@Line = value;
        Discriminator = OutputDiscriminator.@Line;
    }

    internal @Output(@StageDirection value)
    {
        this.@StageDirection = value;
        Discriminator = OutputDiscriminator.@StageDirection;
    }

    internal @Output(@Choice value)
    {
        this.@Choice = value;
        Discriminator = OutputDiscriminator.@Choice;
    }

    public @Line @Line { get; }

    public @StageDirection @StageDirection { get; }

    public @Choice @Choice { get; }

    public OutputDiscriminator Discriminator { get; }

    public object? AsObject()
    {
        switch (Discriminator)
        {
            case OutputDiscriminator.@Line:
                return this.@Line;
            case OutputDiscriminator.@StageDirection:
                return this.@StageDirection;
            case OutputDiscriminator.@Choice:
                return this.@Choice;
        }

        throw new global::System.InvalidOperationException("Invalid discriminator");
    }

    public void Run(global::System.Action<@Line> actionLine, global::System.Action<@StageDirection> actionStageDirection, global::System.Action<@Choice> actionChoice)
    {
        switch (Discriminator)
        {
            case OutputDiscriminator.@Line:
                actionLine(this.@Line);
                return;
            case OutputDiscriminator.@StageDirection:
                actionStageDirection(this.@StageDirection);
                return;
            case OutputDiscriminator.@Choice:
                actionChoice(this.@Choice);
                return;
        }

        throw new global::System.InvalidOperationException("Invalid discriminator");
    }

    public T Evaluate<T>(global::System.Func<@Line, T> functionLine, global::System.Func<@StageDirection, T> functionStageDirection, global::System.Func<@Choice, T> functionChoice)
    {
        switch (Discriminator)
        {
            case OutputDiscriminator.@Line:
                return functionLine(this.@Line);
            case OutputDiscriminator.@StageDirection:
                return functionStageDirection(this.@StageDirection);
            case OutputDiscriminator.@Choice:
                return functionChoice(this.@Choice);
        }

        throw new global::System.InvalidOperationException("Invalid discriminator");
    }

    public bool Equals(@Output other)
    {
        return Discriminator == other.Discriminator && this.@Line == other.@Line && this.@StageDirection == other.@StageDirection && this.@Choice == other.@Choice;
    }

    public override bool Equals(object? other)
    {
        return other is @Output union && Equals(union);
    }

    public override int GetHashCode()
    {
        global::System.HashCode hashcode = default;
        hashcode.Add(this.@Line);
        hashcode.Add(this.@StageDirection);
        hashcode.Add(this.@Choice);
        return hashcode.ToHashCode();
    }

    public static bool operator ==(@Output x, @Output y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(@Output x, @Output y)
    {
        return !x.Equals(y);
    }

    public enum OutputDiscriminator
    {
        @Line,
        @StageDirection,
        @Choice,
    }

    @Line global::Phantonia.Historia.IUnion<@Line, @StageDirection, @Choice>.Value0
    {
        get
        {
            return this.@Line;
        }
    }

    @StageDirection global::Phantonia.Historia.IUnion<@Line, @StageDirection, @Choice>.Value1
    {
        get
        {
            return this.@StageDirection;
        }
    }

    @Choice global::Phantonia.Historia.IUnion<@Line, @StageDirection, @Choice>.Value2
    {
        get
        {
            return this.@Choice;
        }
    }

    int global::Phantonia.Historia.IUnion<@Line, @StageDirection, @Choice>.Discriminator
    {
        get
        {
            return (int)Discriminator;
        }
    }
}

public readonly struct @Option : global::System.IEquatable<@Option>
{
    internal @Option(string? @Text)
    {
        this.@Text = @Text;
    }

    public string? @Text { get; }

    public bool Equals(@Option other)
    {
        return @Text == other.@Text;
    }

    public override bool Equals(object? other)
    {
        return other is @Option record && Equals(record);
    }

    public override int GetHashCode()
    {
        global::System.HashCode hashcode = default;
        hashcode.Add(@Text);
        return hashcode.ToHashCode();
    }

    public static bool operator ==(@Option x, @Option y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(@Option x, @Option y)
    {
        return !x.Equals(y);
    }
}


internal struct Fields
{
    public int state;
    public int outcome446;
    public int outcome499;
    public ulong ls12760;
}

public sealed class HistoriaStoryStateMachine : global::Phantonia.Historia.IStoryStateMachine<@Output, @Option>
{
    public HistoriaStoryStateMachine()
    {
        fields.state = -2;
        options = new @Option[3];
    }

    private int optionsCount;
    private readonly @Option[] options;
    private Fields fields;

    public bool NotStartedStory { get; private set; } = true;

    public bool FinishedStory { get; private set; } = false;

    public global::Phantonia.Historia.ReadOnlyList<@Option> Options
    {
        get
        {
            return new global::Phantonia.Historia.ReadOnlyList<@Option>(options, 0, optionsCount);
        }
    }

    public @Output Output { get; private set; }

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
        @Option[] optionsCopy = new @Option[options.Length];
        global::System.Array.Copy(options, optionsCopy, options.Length);
        return new HistoriaStorySnapshot(fields, Output, optionsCopy, optionsCount);
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

    global::System.Collections.Generic.IReadOnlyList<@Option> global::Phantonia.Historia.IStoryStateMachine<@Output, @Option>.Options
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
            return new global::Phantonia.Historia.ObjectReadOnlyList<@Option>(Options);
        }
    }
    global::Phantonia.Historia.IStorySnapshot global::Phantonia.Historia.IStoryStateMachine.CreateSnapshot()
    {
        return CreateSnapshot();
    }

    global::Phantonia.Historia.IStorySnapshot<@Output, @Option> global::Phantonia.Historia.IStoryStateMachine<@Output, @Option>.CreateSnapshot()
    {
        return CreateSnapshot();
    }
}

public sealed class HistoriaStorySnapshot : global::Phantonia.Historia.IStorySnapshot<@Output, @Option>
{

    internal HistoriaStorySnapshot(Fields fields, @Output output, @Option[] options, int optionsCount)
    {
        this.fields = fields;
        Output = output;
        this.options = options;
        this.optionsCount = optionsCount;
        NotStartedStory = fields.state == -2;
        FinishedStory = fields.state == -1;
    }

    private readonly int optionsCount;
    private readonly @Option[] options;
    internal readonly Fields fields;

    public bool NotStartedStory { get; } = true;

    public bool FinishedStory { get; } = false;

    public global::Phantonia.Historia.ReadOnlyList<@Option> Options
    {
        get
        {
            return new global::Phantonia.Historia.ReadOnlyList<@Option>(options, 0, optionsCount);
        }
    }

    public @Output Output { get; }

    public HistoriaStorySnapshot? TryContinue()
    {
        if (FinishedStory || Options.Count != 0)
        {
            return null;
        }

        Fields fieldsCopy = fields;
        Heart.StateTransition(ref fieldsCopy, 0); @Output output = Heart.GetOutput(ref fieldsCopy);
        @Option[] optionsCopy = new @Option[options.Length];
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
        Heart.StateTransition(ref fieldsCopy, option); @Output output = Heart.GetOutput(ref fieldsCopy);
        @Option[] optionsCopy = new @Option[options.Length];
        int optionsCountCopy = optionsCount;
        Heart.GetOptions(ref fieldsCopy, optionsCopy, ref optionsCountCopy);
        return new HistoriaStorySnapshot(fieldsCopy, output, optionsCopy, optionsCountCopy);
    }

    object global::Phantonia.Historia.IStorySnapshot.Output
    {
        get
        {
            return Output;
        }
    }

    global::System.Collections.Generic.IReadOnlyList<@Option> global::Phantonia.Historia.IStorySnapshot<@Output, @Option>.Options
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
            return new global::Phantonia.Historia.ObjectReadOnlyList<@Option>(Options);
        }
    }

    global::Phantonia.Historia.IStorySnapshot<@Output, @Option>? global::Phantonia.Historia.IStorySnapshot<@Output, @Option>.TryContinue()
    {
        return TryContinue();
    }

    global::Phantonia.Historia.IStorySnapshot<@Output, @Option>? global::Phantonia.Historia.IStorySnapshot<@Output, @Option>.TryContinueWithOption(int option)
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
    public static void StateTransition(ref Fields fields, int option)
    {
        while (true)
        {
            switch (fields.state)
            {
                case -2:
                    fields.state = 672;
                    return;
                case 672:
                    fields.state = 966;
                    return;
                case 966:
                    fields.state = 1065;
                    return;
                case 1065:
                    fields.state = 1128;
                    return;
                case 1128:
                    fields.state = 1354;
                    return;
                case 1354:
                    fields.state = 1408;
                    return;
                case 1408:
                    fields.state = 1500;
                    return;
                case 1500:
                    fields.state = 1563;
                    return;
                case 1563:
                    fields.state = 1820;
                    return;
                case 1820:
                    fields.state = 1893;
                    return;
                case 1893:
                    fields.state = 1956;
                    return;
                case 1956:
                    fields.state = 2027;
                    return;
                case 2027:
                    fields.state = 2487;
                    return;
                case 2487:
                    fields.state = 2611;
                    return;
                case 2611:
                    fields.state = 2734;
                    return;
                case 2734:
                    fields.state = 2770;
                    return;
                case 2770:
                    fields.state = 2911;
                    return;
                case 2911:
                    fields.state = 2947;
                    return;
                case 2947:
                    fields.state = 3046;
                    return;
                case 3046:
                    fields.state = 3171;
                    return;
                case 3171:
                    fields.state = 3367;
                    return;
                case 3367:
                    fields.state = 3424;
                    return;
                case 3424:
                    fields.state = 3651;
                    return;
                case 3651:
                    fields.state = 3916;
                    return;
                case 3916:
                    fields.state = 3985;
                    return;
                case 3985:
                    fields.state = 4254;
                    return;
                case 4254:
                    fields.state = 4483;
                    return;
                case 4483:
                    fields.state = 4831;
                    return;
                case 4831:
                    fields.state = 4925;
                    return;
                case 4925:
                    fields.state = 5634;
                    return;
                case 5634:
                    fields.state = 5790;
                    return;
                case 5790:
                    fields.state = 5879;
                    return;
                case 5879:
                    fields.state = 5976;
                    return;
                case 5976:
                    fields.state = 6044;
                    return;
                case 6044:
                    switch (option)
                    {
                        case 0:
                            fields.state = 6173;
                            return;
                        case 1:
                            fields.state = 6474;
                            return;
                    }

                    break;
                case 6173:
                    fields.state = 6313;
                    return;
                case 6313:
                    fields.state = 6646;
                    return;
                case 6474:
                    fields.state = 6546;
                    return;
                case 6546:
                    fields.state = 6646;
                    return;
                case 6646:
                    fields.state = 6710;
                    return;
                case 6710:
                    fields.state = 6756;
                    return;
                case 6756:
                    fields.state = 6842;
                    return;
                case 6842:
                    fields.state = 6899;
                    return;
                case 6899:
                    switch (option)
                    {
                        case 0:
                            fields.state = 7022;
                            return;
                        case 1:
                            fields.state = 7438;
                            return;
                    }

                    break;
                case 7022:
                    fields.state = 7088;
                    return;
                case 7088:
                    fields.state = 7196;
                    return;
                case 7196:
                    fields.state = 7297;
                    return;
                case 7297:
                    fields.state = 7763;
                    return;
                case 7438:
                    fields.state = 7561;
                    return;
                case 7561:
                    fields.state = 7660;
                    return;
                case 7660:
                    fields.state = 7763;
                    return;
                case 7763:
                    fields.state = 7891;
                    return;
                case 7891:
                    fields.state = 7993;
                    return;
                case 7993:
                    fields.state = 8139;
                    return;
                case 8139:
                    fields.state = 8207;
                    return;
                case 8207:
                    fields.state = 8297;
                    return;
                case 8297:
                    fields.state = 8367;
                    return;
                case 8367:
                    fields.state = 8427;
                    return;
                case 8427:
                    fields.state = 8569;
                    return;
                case 8569:
                    switch (option)
                    {
                        case 0:
                            fields.state = 8695;
                            continue;
                        case 1:
                            fields.state = 9398;
                            continue;
                    }

                    break;
                case 8695:
                    fields.outcome446 = 0;
                    fields.state = 8751;
                    return;
                case 8751:
                    fields.state = 8862;
                    return;
                case 8862:
                    fields.state = 8933;
                    return;
                case 8933:
                    fields.state = 8992;
                    return;
                case 8992:
                    fields.state = 9152;
                    return;
                case 9152:
                    fields.state = 9222;
                    return;
                case 9222:
                    fields.state = 9670;
                    return;
                case 9398:
                    fields.outcome446 = 1;
                    fields.state = 9453;
                    return;
                case 9453:
                    fields.state = 9670;
                    return;
                case 9670:
                    fields.state = 9735;
                    return;
                case 9735:
                    fields.state = 9813;
                    return;
                case 9813:
                    fields.state = 9882;
                    return;
                case 9882:
                    switch (option)
                    {
                        case 0:
                            fields.state = 10021;
                            return;
                        case 1:
                            fields.state = 10267;
                            return;
                    }

                    break;
                case 10021:
                    fields.state = 10137;
                    return;
                case 10137:
                    fields.state = 10463;
                    continue;
                case 10267:
                    fields.state = 10360;
                    return;
                case 10360:
                    fields.state = 10463;
                    continue;
                case 10463:
                    switch (fields.outcome446)
                    {
                        case 0:
                            fields.state = 10557;
                            return;
                        case 1:
                            fields.state = 10912;
                            return;
                    }

                    throw new global::System.InvalidOperationException("Fatal internal error: Invalid outcome");
                case 10557:
                    fields.state = 10627;
                    return;
                case 10627:
                    fields.state = 10694;
                    return;
                case 10694:
                    fields.state = 10794;
                    return;
                case 10794:
                    fields.state = 11672;
                    return;
                case 10912:
                    fields.state = 11034;
                    return;
                case 11034:
                    fields.state = 11167;
                    return;
                case 11167:
                    fields.state = 11242;
                    return;
                case 11242:
                    fields.state = 11356;
                    return;
                case 11356:
                    fields.state = 11418;
                    return;
                case 11418:
                    fields.state = 11479;
                    return;
                case 11479:
                    fields.state = 11596;
                    return;
                case 11596:
                    fields.state = 11672;
                    return;
                case 11672:
                    fields.state = 11775;
                    return;
                case 11775:
                    fields.state = 11942;
                    return;
                case 11942:
                    fields.state = 12015;
                    return;
                case 12015:
                    fields.state = 12081;
                    return;
                case 12081:
                    fields.state = 12162;
                    return;
                case 12162:
                    fields.state = 12281;
                    return;
                case 12281:
                    fields.state = 12352;
                    return;
                case 12352:
                    fields.state = 12453;
                    return;
                case 12453:
                    fields.state = 12504;
                    return;
                case 12504:
                    fields.state = 12610;
                    return;
                case 12610:
                    fields.state = 12701;
                    return;
                case 12701:
                    if (fields.ls12760 == 7)
                    {
                        fields.ls12760 = 0;
                        fields.state = 14578;
                        return;
                    }
                    else
                    {
                        fields.state = 12760;
                        return;
                    }
                case 12760:
                    {
                        int tempOption = option;
                        int realOption = 0;

                        for (int i = 0; i < 64; i++)
                        {
                            if ((fields.ls12760 & (1UL << i)) == 0)
                            {
                                tempOption--;
                            }

                            if (tempOption < 0)
                            {
                                break;
                            }

                            realOption++;
                        }

                        switch (realOption)
                        {
                            case 0:
                                fields.ls12760 |= 1 << 0;
                                fields.state = 12893;
                                return;
                            case 1:
                                fields.ls12760 |= 1 << 1;
                                fields.state = 13378;
                                return;
                            case 2:
                                fields.ls12760 |= 1 << 2;
                                fields.state = 13765;
                                return;
                        }

                        break;
                    }
                case 12893:
                    fields.state = 13010;
                    return;
                case 13010:
                    fields.state = 13082;
                    return;
                case 13082:
                    fields.state = 13137;
                    return;
                case 13137:
                    fields.state = 13237;
                    return;
                case 13237:
                    if (fields.ls12760 == 7)
                    {
                        fields.ls12760 = 0;
                        fields.state = 14578;
                        return;
                    }
                    else
                    {
                        fields.state = 12760;
                        return;
                    }
                case 13378:
                    fields.state = 13503;
                    return;
                case 13503:
                    fields.state = 13605;
                    return;
                case 13605:
                    if (fields.ls12760 == 7)
                    {
                        fields.ls12760 = 0;
                        fields.state = 14578;
                        return;
                    }
                    else
                    {
                        fields.state = 12760;
                        return;
                    }
                case 13765:
                    fields.state = 13953;
                    return;
                case 13953:
                    fields.state = 14032;
                    return;
                case 14032:
                    fields.state = 14093;
                    return;
                case 14093:
                    fields.state = 14156;
                    return;
                case 14156:
                    fields.state = 14373;
                    return;
                case 14373:
                    fields.state = 14482;
                    return;
                case 14482:
                    if (fields.ls12760 == 7)
                    {
                        fields.ls12760 = 0;
                        fields.state = 14578;
                        return;
                    }
                    else
                    {
                        fields.state = 12760;
                        return;
                    }
                case 14578:
                    fields.state = 14646;
                    return;
                case 14646:
                    switch (option)
                    {
                        case 0:
                            fields.state = 14775;
                            continue;
                        case 1:
                            fields.state = 15160;
                            continue;
                        case 2:
                            fields.state = 15699;
                            continue;
                    }

                    break;
                case 14775:
                    fields.outcome499 = 0;
                    fields.state = 14827;
                    return;
                case 14827:
                    fields.state = 14913;
                    return;
                case 14913:
                    fields.state = 15009;
                    return;
                case 15009:
                    fields.state = 16136;
                    return;
                case 15160:
                    fields.outcome499 = 1;
                    fields.state = 15212;
                    return;
                case 15212:
                    fields.state = 15297;
                    return;
                case 15297:
                    fields.state = 15386;
                    return;
                case 15386:
                    fields.state = 15465;
                    return;
                case 15465:
                    fields.state = 15561;
                    return;
                case 15561:
                    fields.state = 16136;
                    return;
                case 15699:
                    fields.outcome499 = 2;
                    fields.state = 15750;
                    return;
                case 15750:
                    fields.state = 15839;
                    return;
                case 15839:
                    fields.state = 15968;
                    return;
                case 15968:
                    fields.state = 16049;
                    return;
                case 16049:
                    fields.state = 16136;
                    return;
                case 16136:
                    fields.state = 16229;
                    return;
                case 16229:
                    fields.state = 16284;
                    return;
                case 16284:
                    fields.state = 16356;
                    return;
                case 16356:
                    fields.state = 16432;
                    return;
                case 16432:
                    fields.state = 16527;
                    return;
                case 16527:
                    fields.state = 16605;
                    continue;
                case 16605:
                    switch (fields.outcome499)
                    {
                        case 0:
                            fields.state = 16695;
                            return;
                        case 1:
                            fields.state = 17259;
                            return;
                        case 2:
                            fields.state = 17819;
                            return;
                    }

                    throw new global::System.InvalidOperationException("Fatal internal error: Invalid outcome");
                case 16695:
                    fields.state = 16773;
                    return;
                case 16773:
                    fields.state = 16831;
                    return;
                case 16831:
                    fields.state = 16891;
                    return;
                case 16891:
                    fields.state = 16975;
                    return;
                case 16975:
                    fields.state = 17039;
                    return;
                case 17039:
                    fields.state = 18355;
                    return;
                case 17259:
                    fields.state = 17336;
                    return;
                case 17336:
                    fields.state = 17394;
                    return;
                case 17394:
                    fields.state = 17454;
                    return;
                case 17454:
                    fields.state = 17537;
                    return;
                case 17537:
                    fields.state = 17601;
                    return;
                case 17601:
                    fields.state = 18355;
                    return;
                case 17819:
                    fields.state = 17900;
                    return;
                case 17900:
                    fields.state = 17958;
                    return;
                case 17958:
                    fields.state = 18018;
                    return;
                case 18018:
                    fields.state = 18105;
                    return;
                case 18105:
                    fields.state = 18169;
                    return;
                case 18169:
                    fields.state = 18355;
                    return;
                case 18355:
                    fields.state = 18424;
                    return;
                case 18424:
                    fields.state = 18530;
                    return;
                case 18530:
                    fields.state = 18647;
                    return;
                case 18647:
                    fields.state = 18762;
                    return;
                case 18762:
                    fields.state = 18863;
                    return;
                case 18863:
                    fields.state = 18982;
                    return;
                case 18982:
                    fields.state = 19048;
                    return;
                case 19048:
                    fields.state = 19155;
                    return;
                case 19155:
                    fields.state = 19213;
                    return;
                case 19213:
                    fields.state = 19289;
                    return;
                case 19289:
                    fields.state = 19381;
                    return;
                case 19381:
                    fields.state = -1;
                    return;
            }

            throw new global::System.InvalidOperationException("Fatal internal error: Invalid state");
        }
    }

    public static @Output GetOutput(ref Fields fields)
    {
        switch (fields.state)
        {
            case 672:
                return new @Output(new @StageDirection("The curtain is closed. Lucy, a young woman with long blond hair that she wears messily in a bun, wearing a long white gown, stands on the edge of stage in front of the curtain, looking down. The stage is not well lit, except for a beam of light directly on Lucy."));
            case 966:
                return new @Output(new @Line(Character.Lucy, "beaming with joy, to the audience", "Welcome, folks, welcome!"));
            case 1065:
                return new @Output(new @StageDirection("Lucy looks up, to the audience."));
            case 1128:
                return new @Output(new @Line(Character.Lucy, "now addressing the audience", "You're about to see an entire spectacle. Truly one of a kind. One that you will never forget, I promise. An entire theater play, all about me. I'm flattered!"));
            case 1354:
                return new @Output(new @StageDirection("Lucy looks down again."));
            case 1408:
                return new @Output(new @Line(Character.Lucy, "now murmurs", "Which is strange. Is that really right?"));
            case 1500:
                return new @Output(new @StageDirection("Lucy looks around, irritatedly."));
            case 1563:
                return new @Output(new @Line(Character.Lucy, "dumbfoundedly", "Why would they make a play about me? That makes no sense. What interesting thing is there to tell about me? It's been four years, surely something happened that is more fascinating than me. After all..."));
            case 1820:
                return new @Output(new @StageDirection("Lucy looks the audience right in the eye."));
            case 1893:
                return new @Output(new @Line(Character.Lucy, "dead serious", "I'm dead."));
            case 1956:
                return new @Output(new @StageDirection("Lucy breaks down."));
            case 2027:
                return new @Output(new @StageDirection("The curtains opens, revealing a lone bed on the right side of stage, and a paper cutout of a house on the left side where the main door is cut out, both angled pointing to the center. The lighting flickers and sounds of a thunderstorm roar. Freedom lies in bed and moves heavily, as if they are having a nightmare. A silhouette walks past the bed with an umbrella and a box in gift wrap paper. They stop in front of the house."));
            case 2487:
                return new @Output(new @StageDirection("Cut. The stage goes dark and the sounds stop. Then the flickering lights and sound continue."));
            case 2611:
                return new @Output(new @StageDirection("A silhouette runs out of the door, to the right, off stage, chased by another silhouette."));
            case 2734:
                return new @Output(new @StageDirection("Cut."));
            case 2770:
                return new @Output(new @StageDirection("A silhouette returns from the right, stops in front of the house, and breaks down crying, which is audible."));
            case 2911:
                return new @Output(new @StageDirection("Cut."));
            case 2947:
                return new @Output(new @StageDirection("The silhouette now stands at the edge of stage and loudly scream."));
            case 3046:
                return new @Output(new @StageDirection("Cut. The thunderstorm has stopped, the stage is now fully lit, and the house has disappeared."));
            case 3171:
                return new @Output(new @StageDirection("Freedom sits down in bed, then gets up and walks to the edge of stage, exactly where the silhouette has just stood. They are wearing their pyjamas and are barefoot."));
            case 3367:
                return new @Output(new @Line(Character.Freedom, " ", "Oh, Lucy!"));
            case 3424:
                return new @Output(new @StageDirection("The background transforms into the glade of a forest. Across the stage there is a fallen tree, lying almost parallel to the edge of stage. Freedom takes a few steps back and catches their breath."));
            case 3651:
                return new @Output(new @Line(Character.Freedom, "to the audience, dreamy", "Lucy, I dreamt of you. Again. *(pause)* Silly of me to tell you, as if you hadn't known that already. I know you're out there, making sure I would never forget you. But how could I ever forget you?"));
            case 3916:
                return new @Output(new @StageDirection("Freedom sits down on the fallen tree."));
            case 3985:
                return new @Output(new @Line(Character.Freedom, " ", "There is something I wanted to tell you. I'm finally doing it. Remember back then, when you wanted to stay here and all I ever wanted was leave, see the world? I waited too long, maybe too long, but I can't wait any longer."));
            case 4254:
                return new @Output(new @Line(Character.Freedom, "contemplative", "I know I have the right to do this, and I will. Why does it still feel like I'm betraying you? My dear Lucy, I will always carry a part of you in my heart, whereever I go."));
            case 4483:
                return new @Output(new @Line(Character.Freedom, " ", "It will break Cay's heart. Honestly, leaving her behind will also break my heart. Ever since you... left... she has been everything to me. Although I probably was a much worse sister to her than the other way around. I need to tell her now, already way later than I should've. Will you keep me company?"));
            case 4831:
                return new @Output(new @StageDirection("Freedom gets up and leaves on the right."));
            case 4925:
                return new @Output(new @StageDirection("The stage transforms to the market place of Blueberry Hill. On the left there is the tavern which states \"Blueberry Ale\" in wooden letters. Two tables with benches stand on either side of the big door. In the middle, there is a street. On the right, there are several residential buildings. There are three market stalls: on the very left, someone is selling garment. On the right, two market stalls are right beside each other. One with a handwritten banner that says \"Mike's Everything & Anything\" that has a lot of hotchpotch, and one with lots of vegetables. Mike is sorting stuff at his stand. Caylee is currently wiping the right of the two tables outside the tavern."));
            case 5634:
                return new @Output(new @StageDirection("Freedom enters from the right. Caylee notices them and leaves her cloth on the table to come up to them and give them a hug."));
            case 5790:
                return new @Output(new @Line(Character.Caylee, " ", "Good morning. You were gone, when I woke up."));
            case 5879:
                return new @Output(new @Line(Character.Freedom, " ", "Good morning, Cay. Yeah, I needed to clear my head."));
            case 5976:
                return new @Output(new @Line(Character.Caylee, " ", "Dreamt of Lucy again?"));
            case 6044:
                return new @Output(new @Choice(Character.Freedom, Severity.Minor));
            case 6173:
                return new @Output(new @Line(Character.Freedom, " ", "Yup, I had a nightmare. The same one I keep having, of the night that... *(they halt)*"));
            case 6313:
                return new @Output(new @Line(Character.Caylee, " ", "I know, Freddie, I know. Come here!"));
            case 6474:
                return new @Output(new @Line(Character.Freedom, " ", "What do you think?"));
            case 6546:
                return new @Output(new @Line(Character.Caylee, " ", "I think that you deserve a big hug!"));
            case 6646:
                return new @Output(new @StageDirection("Caylee gives Freedom a long hug."));
            case 6710:
                return new @Output(new @StageDirection("They let go."));
            case 6756:
                return new @Output(new @Line(Character.Freedom, " ", "There's something we need to talk about."));
            case 6842:
                return new @Output(new @Line(Character.Caylee, " ", "Is it bad?"));
            case 6899:
                return new @Output(new @Choice(Character.Freedom, Severity.Minor));
            case 7022:
                return new @Output(new @Line(Character.Freedom, " ", "Don't worry."));
            case 7088:
                return new @Output(new @Line(Character.Caylee, "slightly joking", "I don't know if I can do that, not worry."));
            case 7196:
                return new @Output(new @Line(Character.Freedom, " ", "Then work on it. Try using this as an exercise."));
            case 7297:
                return new @Output(new @Line(Character.Caylee, "laughs", "Will try."));
            case 7438:
                return new @Output(new @Line(Character.Freedom, " ", "Very bad. Like, extremely bad. If I were you, better start panicking."));
            case 7561:
                return new @Output(new @Line(Character.Caylee, "laughs", "Hey! *(jokingly hits Freedom)* Not funny!"));
            case 7660:
                return new @Output(new @Line(Character.Freedom, " ", "Disagreed, I think it's pretty funny."));
            case 7763:
                return new @Output(new @Line(Character.Caylee, " ", "I really have to work. Can we talk later? Just pick me up from the tavern after it?"));
            case 7891:
                return new @Output(new @Line(Character.Freedom, " ", "Do you not have time for your preciouse sibling Freddie?"));
            case 7993:
                return new @Output(new @Line(Character.Caylee, " ", "I always have time for time, except for when I need to earn a living. Our living, to be quite honest."));
            case 8139:
                return new @Output(new @Line(Character.Freedom, " ", "Hey, I'm contributing."));
            case 8207:
                return new @Output(new @Line(Character.Caylee, "laughing", "It's fine. But let's talk later, yeah?"));
            case 8297:
                return new @Output(new @Line(Character.Freedom, " ", "Your wish is my command."));
            case 8367:
                return new @Output(new @Line(Character.Caylee, " ", "It better be."));
            case 8427:
                return new @Output(new @StageDirection("Caylee gives Freedom one last hug, then turns around and walks towards the table that she was just cleaning."));
            case 8569:
                return new @Output(new @Choice(Character.Freedom, Severity.Major));
            case 8751:
                return new @Output(new @Line(Character.Freedom, "louder, towards Caylee", "I'm leaving Blueberry Hill. Tonight."));
            case 8862:
                return new @Output(new @Line(Character.Caylee, "turns around", "Really?"));
            case 8933:
                return new @Output(new @Line(Character.Freedom, " ", "Yeah."));
            case 8992:
                return new @Output(new @Line(Character.Caylee, " ", "I knew that would happen at some point. *(gulps)* Later we have all the time in the world to talk about it."));
            case 9152:
                return new @Output(new @Line(Character.Freedom, " ", "I will be there."));
            case 9222:
                return new @Output(new @StageDirection("Caylee turns to work again, visibly shaken. Freedom breathes in and out."));
            case 9453:
                return new @Output(new @StageDirection("Freedom watches Caylee clean the table for a few seconds."));
            case 9670:
                return new @Output(new @Line(Character.Mike, "to Freedom", "Hey, Freddie."));
            case 9735:
                return new @Output(new @Line(Character.Freedom, "walking towards Mike's stall", "Mike!"));
            case 9813:
                return new @Output(new @Line(Character.Mike, " ", "Sorry for eavesdropping."));
            case 9882:
                return new @Output(new @Choice(Character.Freedom, Severity.Minor));
            case 10021:
                return new @Output(new @Line(Character.Freedom, " ", "I'm not blaming you for something I so would have done myself."));
            case 10137:
                return new @Output(new @Line(Character.Mike, "smiles", "Great."));
            case 10267:
                return new @Output(new @Line(Character.Freedom, " ", "How will I ever be able to forgive you?"));
            case 10360:
                return new @Output(new @Line(Character.Mike, " ", "I understand if you won't. I deserve it."));
            case 10557:
                return new @Output(new @Line(Character.Mike, " ", "But you're leaving."));
            case 10627:
                return new @Output(new @Line(Character.Freedom, " ", "That is true."));
            case 10694:
                return new @Output(new @Line(Character.Mike, " ", "Actually I'm more surprised you held out so long."));
            case 10794:
                return new @Output(new @Line(Character.Freedom, " ", "Me too, Mike, me too."));
            case 10912:
                return new @Output(new @Line(Character.Mike, " ", "Excuse my curiosity, but what do you want to talk to your sister about?"));
            case 11034:
                return new @Output(new @Line(Character.Freedom, "jokingly", "Excuse *me*, what gives you the impression this is any of your business?"));
            case 11167:
                return new @Output(new @Line(Character.Mike, " ", "Sorry, couldn't help it."));
            case 11242:
                return new @Output(new @Line(Character.Freedom, " ", "It's fine. I'm leaving Blueberry Hill. Tonight and for good."));
            case 11356:
                return new @Output(new @Line(Character.Mike, " ", "Surprising."));
            case 11418:
                return new @Output(new @Line(Character.Freedom, " ", "Really?"));
            case 11479:
                return new @Output(new @Line(Character.Mike, " ", "Yes, incredibly surprising that you didn't already leave ages ago."));
            case 11596:
                return new @Output(new @Line(Character.Freedom, " ", "True that."));
            case 11672:
                return new @Output(new @Line(Character.Mike, " ", "I will miss you as the world's best sorter of bits and bobs."));
            case 11775:
                return new @Output(new @Line(Character.Freedom, " ", "Yeah, Mike, if you keep bringing boxes and boxes of stuff, someone needs to label it all and put everything in its place."));
            case 11942:
                return new @Output(new @Line(Character.Mike, " ", "And you were incredible at it."));
            case 12015:
                return new @Output(new @Line(Character.Freedom, " ", "Mike, It's not hard."));
            case 12081:
                return new @Output(new @Line(Character.Mike, " ", "Still. I know I couldn't pay you much."));
            case 12162:
                return new @Output(new @Line(Character.Freedom, " ", "Yeah, now I will have to actually look for a real job, to sustain myself."));
            case 12281:
                return new @Output(new @Line(Character.Mike, " ", "Life is getting serious now."));
            case 12352:
                return new @Output(new @Line(Character.Freedom, " ", "Oh believe me, my life has been serious enough already."));
            case 12453:
                return new @Output(new @Line(Character.Mike, " ", "I bet."));
            case 12504:
                return new @Output(new @Line(Character.Mike, " ", "Okay, Freddie, choose something, anything, and you can have it."));
            case 12610:
                return new @Output(new @Line(Character.Freedom, " ", "Dangerous, old man. You know I will abuse it."));
            case 12701:
                return new @Output(new @Line(Character.Mike, " ", "Then so be it."));
            case 12759:
                return new @Output(new @Choice(Character.Freedom, Severity.Minor));
            case 12760:
                return new @Output(new @Choice(Character.Freedom, Severity.Minor));
            case 12892:
                return new @Output(new @StageDirection("Freedom takes a small music box and starts turning it. A tune starts playing."));
            case 12893:
                return new @Output(new @StageDirection("Freedom takes a small music box and starts turning it. A tune starts playing."));
            case 13009:
                return new @Output(new @Line(Character.Freedom, " ", "Is this a lullaby?"));
            case 13010:
                return new @Output(new @Line(Character.Freedom, " ", "Is this a lullaby?"));
            case 13081:
                return new @Output(new @Line(Character.Mike, " ", "Yup."));
            case 13082:
                return new @Output(new @Line(Character.Mike, " ", "Yup."));
            case 13136:
                return new @Output(new @Line(Character.Freedom, " ", "Maybe I should get this. I always can't sleep."));
            case 13137:
                return new @Output(new @Line(Character.Freedom, " ", "Maybe I should get this. I always can't sleep."));
            case 13236:
                return new @Output(new @StageDirection("Freedom puts the music box back."));
            case 13237:
                return new @Output(new @StageDirection("Freedom puts the music box back."));
            case 13377:
                return new @Output(new @StageDirection("Freedom takes the figurine of a werewolf in the middle of an attack, mouth wide open."));
            case 13378:
                return new @Output(new @StageDirection("Freedom takes the figurine of a werewolf in the middle of an attack, mouth wide open."));
            case 13502:
                return new @Output(new @Line(Character.Freedom, " ", "ROAR! Take care, Mike, because I will *eat* you."));
            case 13503:
                return new @Output(new @Line(Character.Freedom, " ", "ROAR! Take care, Mike, because I will *eat* you."));
            case 13604:
                return new @Output(new @Line(Character.Mike, " ", "Careful, you're scaring the old man."));
            case 13605:
                return new @Output(new @Line(Character.Mike, " ", "Careful, you're scaring the old man."));
            case 13764:
                return new @Output(new @StageDirection("Freedom takes a black bag and opens it. Inside there are a few golden looking coins each with a diameter of about 2cm. They keep them on their hand."));
            case 13765:
                return new @Output(new @StageDirection("Freedom takes a black bag and opens it. Inside there are a few golden looking coins each with a diameter of about 2cm. They keep them on their hand."));
            case 13952:
                return new @Output(new @Line(Character.Freedom, " ", "How much are these worth?"));
            case 13953:
                return new @Output(new @Line(Character.Freedom, " ", "How much are these worth?"));
            case 14031:
                return new @Output(new @Line(Character.Mike, " ", "Worthless."));
            case 14032:
                return new @Output(new @Line(Character.Mike, " ", "Worthless."));
            case 14092:
                return new @Output(new @Line(Character.Freedom, " ", "Very sad."));
            case 14093:
                return new @Output(new @Line(Character.Freedom, " ", "Very sad."));
            case 14155:
                return new @Output(new @Line(Character.Mike, " ", "They are props. But they're very good when you need to make a difficult choice. Look, this one has a happy and a frowny face on it. Or this one, with a cat and a dog."));
            case 14156:
                return new @Output(new @Line(Character.Mike, " ", "They are props. But they're very good when you need to make a difficult choice. Look, this one has a happy and a frowny face on it. Or this one, with a cat and a dog."));
            case 14372:
                return new @Output(new @StageDirection("Freedom takes one of the coins and flips it. They look at the result."));
            case 14373:
                return new @Output(new @StageDirection("Freedom takes one of the coins and flips it. They look at the result."));
            case 14481:
                return new @Output(new @Line(Character.Freedom, " ", "Sad. Yeah, checks out, me too."));
            case 14482:
                return new @Output(new @Line(Character.Freedom, " ", "Sad. Yeah, checks out, me too."));
            case 14578:
                return new @Output(new @Line(Character.Mike, " ", "So, what do you choose?"));
            case 14646:
                return new @Output(new @Choice(Character.Freedom, Severity.Minor));
            case 14827:
                return new @Output(new @StageDirection("Freedom takes the music box and stows it away."));
            case 14913:
                return new @Output(new @Line(Character.Freedom, " ", "I'm taking the music box. I like sleeping."));
            case 15009:
                return new @Output(new @Line(Character.Mike, " ", "Very well. Have fun with that."));
            case 15212:
                return new @Output(new @StageDirection("Freedom takes the figurine and stows it away."));
            case 15297:
                return new @Output(new @Line(Character.Freedom, " ", "This is basically my spirit animal."));
            case 15386:
                return new @Output(new @Line(Character.Mike, " ", "Are werewolves even animals?"));
            case 15465:
                return new @Output(new @Line(Character.Freedom, " ", "I don't know Mike, are werewolves animals?"));
            case 15561:
                return new @Output(new @Line(Character.Mike, " ", "I don't know."));
            case 15750:
                return new @Output(new @StageDirection("Freedom takes the bag of coins and stows it away."));
            case 15839:
                return new @Output(new @Line(Character.Freedom, " ", "I am notoriously bad at making choices. Those will be vital to my survival."));
            case 15968:
                return new @Output(new @Line(Character.Mike, " ", "But you just made this choice."));
            case 16049:
                return new @Output(new @Line(Character.Freedom, " ", "I guess that is true."));
            case 16136:
                return new @Output(new @Line(Character.Freedom, " ", "Thanks Mike. Not just for this, for everything."));
            case 16229:
                return new @Output(new @Line(Character.Mike, " ", "Of course."));
            case 16284:
                return new @Output(new @StageDirection("Freedom's eyes land on a pair of chests."));
            case 16356:
                return new @Output(new @Line(Character.Freedom, " ", "These chests are quite pretty."));
            case 16432:
                return new @Output(new @Line(Character.Mike, " ", "They aren't just pretty. They are quite handy. Look."));
            case 16527:
                return new @Output(new @StageDirection("Mike takes the chests and hands Freedom one."));
            case 16695:
                return new @Output(new @Line(Character.Mike, " ", "Put your music box in here."));
            case 16773:
                return new @Output(new @Line(Character.Freedom, " ", "Why?"));
            case 16831:
                return new @Output(new @Line(Character.Mike, " ", "Trust me."));
            case 16891:
                return new @Output(new @StageDirection("Freedom puts their music box into the chest."));
            case 16975:
                return new @Output(new @Line(Character.Mike, " ", "Now close it."));
            case 17039:
                return new @Output(new @StageDirection("Freedom closes the lid of the chest. Mike opens his chest and pulls out the music box. When Freedom opens their chest, it is empty."));
            case 17259:
                return new @Output(new @Line(Character.Mike, " ", "Put your figurine in here."));
            case 17336:
                return new @Output(new @Line(Character.Freedom, " ", "Why?"));
            case 17394:
                return new @Output(new @Line(Character.Mike, " ", "Trust me."));
            case 17454:
                return new @Output(new @StageDirection("Freedom puts their figurine into the chest."));
            case 17537:
                return new @Output(new @Line(Character.Mike, " ", "Now close it."));
            case 17601:
                return new @Output(new @StageDirection("Freedom closes the lid of the chest. Mike opens his chest and pulls out the figurine. When Freedom opens their chest, it is empty."));
            case 17819:
                return new @Output(new @Line(Character.Mike, " ", "Put your bag of coins in here."));
            case 17900:
                return new @Output(new @Line(Character.Freedom, " ", "Why?"));
            case 17958:
                return new @Output(new @Line(Character.Mike, " ", "Trust me."));
            case 18018:
                return new @Output(new @StageDirection("Freedom puts their bag of coins into the chest."));
            case 18105:
                return new @Output(new @Line(Character.Mike, " ", "Now close it."));
            case 18169:
                return new @Output(new @StageDirection("Freedom closes the lid of the chest. Mike opens his chest and pulls out the bag of coins. When Freedom opens their chest, it is empty."));
            case 18355:
                return new @Output(new @Line(Character.Freedom, " ", "Wow, that is very cool."));
            case 18424:
                return new @Output(new @Line(Character.Mike, " ", "It is, isn't it. And it works across any distance, just *poof*."));
            case 18530:
                return new @Output(new @Line(Character.Freedom, " ", "Over any distance, you say? I could use this to stay in touch with Cay."));
            case 18647:
                return new @Output(new @Line(Character.Mike, " ", "You could, yeah. Just put a letter in here, and Caylee will pull it out."));
            case 18762:
                return new @Output(new @Line(Character.Freedom, " ", "I've changed my mind. I will take these chests instead."));
            case 18863:
                return new @Output(new @Line(Character.Mike, " ", "Know what, I feel generous today. Take these and keep what you already have."));
            case 18982:
                return new @Output(new @Line(Character.Freedom, " ", "You're way too kind."));
            case 19048:
                return new @Output(new @Line(Character.Mike, " ", "I know, I know. Take care of yourself when you travel, will you?"));
            case 19155:
                return new @Output(new @Line(Character.Freedom, " ", "You know me."));
            case 19213:
                return new @Output(new @Line(Character.Mike, " ", "Yeah. That's why I'm saying this."));
            case 19289:
                return new @Output(new @Line(Character.Freedom, " ", "Wow okay. Yeah, I'm just leaving. Bye, Mike!"));
            case 19381:
                return new @Output(new @StageDirection("Freedom leaves Mike's stall and walks to the edge of the stage."));
            case -1:
                return default;
        }

        throw new global::System.InvalidOperationException("Invalid state");
    }

    public static void GetOptions(ref Fields fields, @Option[] options, ref int optionsCount)
    {
        switch (fields.state)
        {
            case 6044:
                global::System.Array.Clear(options);
                options[0] = new @Option("\"Yes, a nightmare\"");
                options[1] = new @Option("\"What do you think?\"");
                optionsCount = 2;
                return;
            case 6899:
                global::System.Array.Clear(options);
                options[0] = new @Option("\"Not really\"");
                options[1] = new @Option("\"Very bad (jokingly)\"");
                optionsCount = 2;
                return;
            case 8569:
                global::System.Array.Clear(options);
                options[0] = new @Option("\"I'm leaving\"");
                options[1] = new @Option("Say nothing");
                optionsCount = 2;
                return;
            case 9882:
                global::System.Array.Clear(options);
                options[0] = new @Option("\"I would've done the same\"");
                options[1] = new @Option("\"How dare you?\"");
                optionsCount = 2;
                return;
            case 12759:
                {
                    global::System.Array.Clear(options);
                    int i = 0;

                    if ((fields.ls12760 & (1UL << 0)) == 0)
                    {
                        options[i] = new @Option("Look at music box");
                        i++;
                    }

                    if ((fields.ls12760 & (1UL << 1)) == 0)
                    {
                        options[i] = new @Option("Look at figurine");
                        i++;
                    }

                    if ((fields.ls12760 & (1UL << 2)) == 0)
                    {
                        options[i] = new @Option("Look at bag of coins");
                        i++;
                    }

                    optionsCount = i;
                    return;
                }
            case 12760:
                {
                    global::System.Array.Clear(options);
                    int i = 0;

                    if ((fields.ls12760 & (1UL << 0)) == 0)
                    {
                        options[i] = new @Option("Look at music box");
                        i++;
                    }

                    if ((fields.ls12760 & (1UL << 1)) == 0)
                    {
                        options[i] = new @Option("Look at figurine");
                        i++;
                    }

                    if ((fields.ls12760 & (1UL << 2)) == 0)
                    {
                        options[i] = new @Option("Look at bag of coins");
                        i++;
                    }

                    optionsCount = i;
                    return;
                }
            case 14646:
                global::System.Array.Clear(options);
                options[0] = new @Option("Take the music box");
                options[1] = new @Option("Take the figurine");
                options[2] = new @Option("Take the bag of coins");
                optionsCount = 3;
                return;
        }

        optionsCount = 0;
    }
}

public static class HistoriaStoryGraph
{
    public static global::Phantonia.Historia.StoryGraph<@Output, @Option> CreateStoryGraph()
    {
        global::System.Collections.Generic.Dictionary<int, global::Phantonia.Historia.StoryVertex<@Output, @Option>> vertices = new global::System.Collections.Generic.Dictionary<int, global::Phantonia.Historia.StoryVertex<@Output, @Option>>(164);

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(966, 672, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(672, -2, false),
            };

            vertices[672] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(672, new @Output(new @StageDirection("The curtain is closed. Lucy, a young woman with long blond hair that she wears messily in a bun, wearing a long white gown, stands on the edge of stage in front of the curtain, looking down. The stage is not well lit, except for a beam of light directly on Lucy.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1065, 966, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(966, 672, false),
            };

            vertices[966] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(966, new @Output(new @Line(Character.Lucy, "beaming with joy, to the audience", "Welcome, folks, welcome!")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1128, 1065, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1065, 966, false),
            };

            vertices[1065] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(1065, new @Output(new @StageDirection("Lucy looks up, to the audience.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1354, 1128, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1128, 1065, false),
            };

            vertices[1128] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(1128, new @Output(new @Line(Character.Lucy, "now addressing the audience", "You're about to see an entire spectacle. Truly one of a kind. One that you will never forget, I promise. An entire theater play, all about me. I'm flattered!")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1408, 1354, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1354, 1128, false),
            };

            vertices[1354] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(1354, new @Output(new @StageDirection("Lucy looks down again.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1500, 1408, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1408, 1354, false),
            };

            vertices[1408] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(1408, new @Output(new @Line(Character.Lucy, "now murmurs", "Which is strange. Is that really right?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1563, 1500, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1500, 1408, false),
            };

            vertices[1500] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(1500, new @Output(new @StageDirection("Lucy looks around, irritatedly.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1820, 1563, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1563, 1500, false),
            };

            vertices[1563] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(1563, new @Output(new @Line(Character.Lucy, "dumbfoundedly", "Why would they make a play about me? That makes no sense. What interesting thing is there to tell about me? It's been four years, surely something happened that is more fascinating than me. After all...")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1893, 1820, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1820, 1563, false),
            };

            vertices[1820] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(1820, new @Output(new @StageDirection("Lucy looks the audience right in the eye.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1956, 1893, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1893, 1820, false),
            };

            vertices[1893] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(1893, new @Output(new @Line(Character.Lucy, "dead serious", "I'm dead.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2027, 1956, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(1956, 1893, false),
            };

            vertices[1956] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(1956, new @Output(new @StageDirection("Lucy breaks down.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2487, 2027, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2027, 1956, false),
            };

            vertices[2027] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(2027, new @Output(new @StageDirection("The curtains opens, revealing a lone bed on the right side of stage, and a paper cutout of a house on the left side where the main door is cut out, both angled pointing to the center. The lighting flickers and sounds of a thunderstorm roar. Freedom lies in bed and moves heavily, as if they are having a nightmare. A silhouette walks past the bed with an umbrella and a box in gift wrap paper. They stop in front of the house.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2611, 2487, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2487, 2027, false),
            };

            vertices[2487] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(2487, new @Output(new @StageDirection("Cut. The stage goes dark and the sounds stop. Then the flickering lights and sound continue.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2734, 2611, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2611, 2487, false),
            };

            vertices[2611] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(2611, new @Output(new @StageDirection("A silhouette runs out of the door, to the right, off stage, chased by another silhouette.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2770, 2734, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2734, 2611, false),
            };

            vertices[2734] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(2734, new @Output(new @StageDirection("Cut.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2911, 2770, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2770, 2734, false),
            };

            vertices[2770] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(2770, new @Output(new @StageDirection("A silhouette returns from the right, stops in front of the house, and breaks down crying, which is audible.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2947, 2911, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2911, 2770, false),
            };

            vertices[2911] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(2911, new @Output(new @StageDirection("Cut.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3046, 2947, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(2947, 2911, false),
            };

            vertices[2947] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(2947, new @Output(new @StageDirection("The silhouette now stands at the edge of stage and loudly scream.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3171, 3046, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3046, 2947, false),
            };

            vertices[3046] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(3046, new @Output(new @StageDirection("Cut. The thunderstorm has stopped, the stage is now fully lit, and the house has disappeared.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3367, 3171, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3171, 3046, false),
            };

            vertices[3171] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(3171, new @Output(new @StageDirection("Freedom sits down in bed, then gets up and walks to the edge of stage, exactly where the silhouette has just stood. They are wearing their pyjamas and are barefoot.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3424, 3367, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3367, 3171, false),
            };

            vertices[3367] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(3367, new @Output(new @Line(Character.Freedom, " ", "Oh, Lucy!")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3651, 3424, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3424, 3367, false),
            };

            vertices[3424] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(3424, new @Output(new @StageDirection("The background transforms into the glade of a forest. Across the stage there is a fallen tree, lying almost parallel to the edge of stage. Freedom takes a few steps back and catches their breath.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3916, 3651, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3651, 3424, false),
            };

            vertices[3651] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(3651, new @Output(new @Line(Character.Freedom, "to the audience, dreamy", "Lucy, I dreamt of you. Again. *(pause)* Silly of me to tell you, as if you hadn't known that already. I know you're out there, making sure I would never forget you. But how could I ever forget you?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3985, 3916, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3916, 3651, false),
            };

            vertices[3916] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(3916, new @Output(new @StageDirection("Freedom sits down on the fallen tree.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(4254, 3985, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(3985, 3916, false),
            };

            vertices[3985] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(3985, new @Output(new @Line(Character.Freedom, " ", "There is something I wanted to tell you. I'm finally doing it. Remember back then, when you wanted to stay here and all I ever wanted was leave, see the world? I waited too long, maybe too long, but I can't wait any longer.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(4483, 4254, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(4254, 3985, false),
            };

            vertices[4254] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(4254, new @Output(new @Line(Character.Freedom, "contemplative", "I know I have the right to do this, and I will. Why does it still feel like I'm betraying you? My dear Lucy, I will always carry a part of you in my heart, whereever I go.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(4831, 4483, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(4483, 4254, false),
            };

            vertices[4483] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(4483, new @Output(new @Line(Character.Freedom, " ", "It will break Cay's heart. Honestly, leaving her behind will also break my heart. Ever since you... left... she has been everything to me. Although I probably was a much worse sister to her than the other way around. I need to tell her now, already way later than I should've. Will you keep me company?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(4925, 4831, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(4831, 4483, false),
            };

            vertices[4831] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(4831, new @Output(new @StageDirection("Freedom gets up and leaves on the right.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(5634, 4925, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(4925, 4831, false),
            };

            vertices[4925] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(4925, new @Output(new @StageDirection("The stage transforms to the market place of Blueberry Hill. On the left there is the tavern which states \"Blueberry Ale\" in wooden letters. Two tables with benches stand on either side of the big door. In the middle, there is a street. On the right, there are several residential buildings. There are three market stalls: on the very left, someone is selling garment. On the right, two market stalls are right beside each other. One with a handwritten banner that says \"Mike's Everything & Anything\" that has a lot of hotchpotch, and one with lots of vegetables. Mike is sorting stuff at his stand. Caylee is currently wiping the right of the two tables outside the tavern.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(5790, 5634, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(5634, 4925, false),
            };

            vertices[5634] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(5634, new @Output(new @StageDirection("Freedom enters from the right. Caylee notices them and leaves her cloth on the table to come up to them and give them a hug.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(5879, 5790, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(5790, 5634, false),
            };

            vertices[5790] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(5790, new @Output(new @Line(Character.Caylee, " ", "Good morning. You were gone, when I woke up.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(5976, 5879, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(5879, 5790, false),
            };

            vertices[5879] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(5879, new @Output(new @Line(Character.Freedom, " ", "Good morning, Cay. Yeah, I needed to clear my head.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6044, 5976, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(5976, 5879, false),
            };

            vertices[5976] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(5976, new @Output(new @Line(Character.Caylee, " ", "Dreamt of Lucy again?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            @Option[] options = { new @Option("\"Yes, a nightmare\""), new @Option("\"What do you think?\""), };

            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6173, 6044, false),
                new global::Phantonia.Historia.StoryEdge(6474, 6044, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6044, 5976, false),
            };

            vertices[6044] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(6044, new @Output(new @Choice(Character.Freedom, Severity.Minor)), new global::Phantonia.Historia.ReadOnlyList<@Option>(options), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6313, 6173, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6173, 6044, false),
            };

            vertices[6173] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(6173, new @Output(new @Line(Character.Freedom, " ", "Yup, I had a nightmare. The same one I keep having, of the night that... *(they halt)*")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6646, 6313, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6313, 6173, false),
            };

            vertices[6313] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(6313, new @Output(new @Line(Character.Caylee, " ", "I know, Freddie, I know. Come here!")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6546, 6474, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6474, 6044, false),
            };

            vertices[6474] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(6474, new @Output(new @Line(Character.Freedom, " ", "What do you think?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6646, 6546, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6546, 6474, false),
            };

            vertices[6546] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(6546, new @Output(new @Line(Character.Caylee, " ", "I think that you deserve a big hug!")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6710, 6646, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6646, 6313, false),
                new global::Phantonia.Historia.StoryEdge(6646, 6546, false),
            };

            vertices[6646] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(6646, new @Output(new @StageDirection("Caylee gives Freedom a long hug.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6756, 6710, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6710, 6646, false),
            };

            vertices[6710] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(6710, new @Output(new @StageDirection("They let go.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6842, 6756, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6756, 6710, false),
            };

            vertices[6756] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(6756, new @Output(new @Line(Character.Freedom, " ", "There's something we need to talk about.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6899, 6842, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6842, 6756, false),
            };

            vertices[6842] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(6842, new @Output(new @Line(Character.Caylee, " ", "Is it bad?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            @Option[] options = { new @Option("\"Not really\""), new @Option("\"Very bad (jokingly)\""), };

            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7022, 6899, false),
                new global::Phantonia.Historia.StoryEdge(7438, 6899, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(6899, 6842, false),
            };

            vertices[6899] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(6899, new @Output(new @Choice(Character.Freedom, Severity.Minor)), new global::Phantonia.Historia.ReadOnlyList<@Option>(options), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7088, 7022, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7022, 6899, false),
            };

            vertices[7022] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(7022, new @Output(new @Line(Character.Freedom, " ", "Don't worry.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7196, 7088, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7088, 7022, false),
            };

            vertices[7088] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(7088, new @Output(new @Line(Character.Caylee, "slightly joking", "I don't know if I can do that, not worry.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7297, 7196, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7196, 7088, false),
            };

            vertices[7196] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(7196, new @Output(new @Line(Character.Freedom, " ", "Then work on it. Try using this as an exercise.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7763, 7297, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7297, 7196, false),
            };

            vertices[7297] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(7297, new @Output(new @Line(Character.Caylee, "laughs", "Will try.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7561, 7438, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7438, 6899, false),
            };

            vertices[7438] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(7438, new @Output(new @Line(Character.Freedom, " ", "Very bad. Like, extremely bad. If I were you, better start panicking.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7660, 7561, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7561, 7438, false),
            };

            vertices[7561] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(7561, new @Output(new @Line(Character.Caylee, "laughs", "Hey! *(jokingly hits Freedom)* Not funny!")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7763, 7660, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7660, 7561, false),
            };

            vertices[7660] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(7660, new @Output(new @Line(Character.Freedom, " ", "Disagreed, I think it's pretty funny.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7891, 7763, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7763, 7297, false),
                new global::Phantonia.Historia.StoryEdge(7763, 7660, false),
            };

            vertices[7763] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(7763, new @Output(new @Line(Character.Caylee, " ", "I really have to work. Can we talk later? Just pick me up from the tavern after it?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7993, 7891, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7891, 7763, false),
            };

            vertices[7891] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(7891, new @Output(new @Line(Character.Freedom, " ", "Do you not have time for your preciouse sibling Freddie?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8139, 7993, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(7993, 7891, false),
            };

            vertices[7993] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(7993, new @Output(new @Line(Character.Caylee, " ", "I always have time for time, except for when I need to earn a living. Our living, to be quite honest.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8207, 8139, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8139, 7993, false),
            };

            vertices[8139] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(8139, new @Output(new @Line(Character.Freedom, " ", "Hey, I'm contributing.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8297, 8207, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8207, 8139, false),
            };

            vertices[8207] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(8207, new @Output(new @Line(Character.Caylee, "laughing", "It's fine. But let's talk later, yeah?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8367, 8297, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8297, 8207, false),
            };

            vertices[8297] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(8297, new @Output(new @Line(Character.Freedom, " ", "Your wish is my command.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8427, 8367, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8367, 8297, false),
            };

            vertices[8367] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(8367, new @Output(new @Line(Character.Caylee, " ", "It better be.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8569, 8427, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8427, 8367, false),
            };

            vertices[8427] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(8427, new @Output(new @StageDirection("Caylee gives Freedom one last hug, then turns around and walks towards the table that she was just cleaning.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            @Option[] options = { new @Option("\"I'm leaving\""), new @Option("Say nothing"), };

            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8751, 8569, false),
                new global::Phantonia.Historia.StoryEdge(9453, 8569, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8569, 8427, false),
            };

            vertices[8569] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(8569, new @Output(new @Choice(Character.Freedom, Severity.Major)), new global::Phantonia.Historia.ReadOnlyList<@Option>(options), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8862, 8751, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8751, 8569, false),
            };

            vertices[8751] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(8751, new @Output(new @Line(Character.Freedom, "louder, towards Caylee", "I'm leaving Blueberry Hill. Tonight.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8933, 8862, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8862, 8751, false),
            };

            vertices[8862] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(8862, new @Output(new @Line(Character.Caylee, "turns around", "Really?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8992, 8933, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8933, 8862, false),
            };

            vertices[8933] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(8933, new @Output(new @Line(Character.Freedom, " ", "Yeah.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9152, 8992, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(8992, 8933, false),
            };

            vertices[8992] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(8992, new @Output(new @Line(Character.Caylee, " ", "I knew that would happen at some point. *(gulps)* Later we have all the time in the world to talk about it.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9222, 9152, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9152, 8992, false),
            };

            vertices[9152] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(9152, new @Output(new @Line(Character.Freedom, " ", "I will be there.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9670, 9222, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9222, 9152, false),
            };

            vertices[9222] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(9222, new @Output(new @StageDirection("Caylee turns to work again, visibly shaken. Freedom breathes in and out.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9670, 9453, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9453, 8569, false),
            };

            vertices[9453] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(9453, new @Output(new @StageDirection("Freedom watches Caylee clean the table for a few seconds.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9735, 9670, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9670, 9222, false),
                new global::Phantonia.Historia.StoryEdge(9670, 9453, false),
            };

            vertices[9670] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(9670, new @Output(new @Line(Character.Mike, "to Freedom", "Hey, Freddie.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9813, 9735, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9735, 9670, false),
            };

            vertices[9735] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(9735, new @Output(new @Line(Character.Freedom, "walking towards Mike's stall", "Mike!")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9882, 9813, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9813, 9735, false),
            };

            vertices[9813] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(9813, new @Output(new @Line(Character.Mike, " ", "Sorry for eavesdropping.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            @Option[] options = { new @Option("\"I would've done the same\""), new @Option("\"How dare you?\""), };

            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10021, 9882, false),
                new global::Phantonia.Historia.StoryEdge(10267, 9882, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(9882, 9813, false),
            };

            vertices[9882] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(9882, new @Output(new @Choice(Character.Freedom, Severity.Minor)), new global::Phantonia.Historia.ReadOnlyList<@Option>(options), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10137, 10021, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10021, 9882, false),
            };

            vertices[10021] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(10021, new @Output(new @Line(Character.Freedom, " ", "I'm not blaming you for something I so would have done myself.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10557, 10137, false),
                new global::Phantonia.Historia.StoryEdge(10912, 10137, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10137, 10021, false),
            };

            vertices[10137] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(10137, new @Output(new @Line(Character.Mike, "smiles", "Great.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10360, 10267, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10267, 9882, false),
            };

            vertices[10267] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(10267, new @Output(new @Line(Character.Freedom, " ", "How will I ever be able to forgive you?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10557, 10360, false),
                new global::Phantonia.Historia.StoryEdge(10912, 10360, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10360, 10267, false),
            };

            vertices[10360] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(10360, new @Output(new @Line(Character.Mike, " ", "I understand if you won't. I deserve it.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10627, 10557, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10557, 10137, false),
                new global::Phantonia.Historia.StoryEdge(10557, 10360, false),
            };

            vertices[10557] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(10557, new @Output(new @Line(Character.Mike, " ", "But you're leaving.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10694, 10627, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10627, 10557, false),
            };

            vertices[10627] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(10627, new @Output(new @Line(Character.Freedom, " ", "That is true.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10794, 10694, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10694, 10627, false),
            };

            vertices[10694] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(10694, new @Output(new @Line(Character.Mike, " ", "Actually I'm more surprised you held out so long.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11672, 10794, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10794, 10694, false),
            };

            vertices[10794] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(10794, new @Output(new @Line(Character.Freedom, " ", "Me too, Mike, me too.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11034, 10912, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(10912, 10137, false),
                new global::Phantonia.Historia.StoryEdge(10912, 10360, false),
            };

            vertices[10912] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(10912, new @Output(new @Line(Character.Mike, " ", "Excuse my curiosity, but what do you want to talk to your sister about?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11167, 11034, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11034, 10912, false),
            };

            vertices[11034] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(11034, new @Output(new @Line(Character.Freedom, "jokingly", "Excuse *me*, what gives you the impression this is any of your business?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11242, 11167, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11167, 11034, false),
            };

            vertices[11167] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(11167, new @Output(new @Line(Character.Mike, " ", "Sorry, couldn't help it.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11356, 11242, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11242, 11167, false),
            };

            vertices[11242] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(11242, new @Output(new @Line(Character.Freedom, " ", "It's fine. I'm leaving Blueberry Hill. Tonight and for good.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11418, 11356, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11356, 11242, false),
            };

            vertices[11356] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(11356, new @Output(new @Line(Character.Mike, " ", "Surprising.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11479, 11418, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11418, 11356, false),
            };

            vertices[11418] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(11418, new @Output(new @Line(Character.Freedom, " ", "Really?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11596, 11479, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11479, 11418, false),
            };

            vertices[11479] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(11479, new @Output(new @Line(Character.Mike, " ", "Yes, incredibly surprising that you didn't already leave ages ago.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11672, 11596, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11596, 11479, false),
            };

            vertices[11596] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(11596, new @Output(new @Line(Character.Freedom, " ", "True that.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11775, 11672, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11672, 10794, false),
                new global::Phantonia.Historia.StoryEdge(11672, 11596, false),
            };

            vertices[11672] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(11672, new @Output(new @Line(Character.Mike, " ", "I will miss you as the world's best sorter of bits and bobs.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11942, 11775, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11775, 11672, false),
            };

            vertices[11775] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(11775, new @Output(new @Line(Character.Freedom, " ", "Yeah, Mike, if you keep bringing boxes and boxes of stuff, someone needs to label it all and put everything in its place.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12015, 11942, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(11942, 11775, false),
            };

            vertices[11942] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(11942, new @Output(new @Line(Character.Mike, " ", "And you were incredible at it.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12081, 12015, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12015, 11942, false),
            };

            vertices[12015] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12015, new @Output(new @Line(Character.Freedom, " ", "Mike, It's not hard.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12162, 12081, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12081, 12015, false),
            };

            vertices[12081] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12081, new @Output(new @Line(Character.Mike, " ", "Still. I know I couldn't pay you much.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12281, 12162, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12162, 12081, false),
            };

            vertices[12162] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12162, new @Output(new @Line(Character.Freedom, " ", "Yeah, now I will have to actually look for a real job, to sustain myself.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12352, 12281, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12281, 12162, false),
            };

            vertices[12281] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12281, new @Output(new @Line(Character.Mike, " ", "Life is getting serious now.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12453, 12352, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12352, 12281, false),
            };

            vertices[12352] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12352, new @Output(new @Line(Character.Freedom, " ", "Oh believe me, my life has been serious enough already.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12504, 12453, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12453, 12352, false),
            };

            vertices[12453] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12453, new @Output(new @Line(Character.Mike, " ", "I bet.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12610, 12504, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12504, 12453, false),
            };

            vertices[12504] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12504, new @Output(new @Line(Character.Mike, " ", "Okay, Freddie, choose something, anything, and you can have it.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12701, 12610, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12610, 12504, false),
            };

            vertices[12610] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12610, new @Output(new @Line(Character.Freedom, " ", "Dangerous, old man. You know I will abuse it.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12760, 12701, false),
                new global::Phantonia.Historia.StoryEdge(12760, 12701, false),
                new global::Phantonia.Historia.StoryEdge(12760, 12701, false),
                new global::Phantonia.Historia.StoryEdge(12760, 12701, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12701, 12610, false),
            };

            vertices[12701] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12701, new @Output(new @Line(Character.Mike, " ", "Then so be it.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12893, 12760, false),
                new global::Phantonia.Historia.StoryEdge(13378, 12760, false),
                new global::Phantonia.Historia.StoryEdge(13765, 12760, false),
                new global::Phantonia.Historia.StoryEdge(14578, 12760, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12760, 12701, false),
                new global::Phantonia.Historia.StoryEdge(12760, 12701, false),
                new global::Phantonia.Historia.StoryEdge(12760, 12701, false),
                new global::Phantonia.Historia.StoryEdge(12760, 12701, false),
                new global::Phantonia.Historia.StoryEdge(12760, 13237, true),
                new global::Phantonia.Historia.StoryEdge(12760, 13605, true),
                new global::Phantonia.Historia.StoryEdge(12760, 14482, true),
            };

            vertices[12760] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12760, new @Output(new @Choice(Character.Freedom, Severity.Minor)), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13010, 12893, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12893, 12760, false),
            };

            vertices[12893] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(12893, new @Output(new @StageDirection("Freedom takes a small music box and starts turning it. A tune starts playing.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13082, 13010, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13010, 12893, false),
            };

            vertices[13010] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(13010, new @Output(new @Line(Character.Freedom, " ", "Is this a lullaby?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13137, 13082, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13082, 13010, false),
            };

            vertices[13082] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(13082, new @Output(new @Line(Character.Mike, " ", "Yup.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13237, 13137, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13137, 13082, false),
            };

            vertices[13137] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(13137, new @Output(new @Line(Character.Freedom, " ", "Maybe I should get this. I always can't sleep.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12760, 13237, true),
                new global::Phantonia.Historia.StoryEdge(14578, 13237, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13237, 13137, false),
            };

            vertices[13237] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(13237, new @Output(new @StageDirection("Freedom puts the music box back.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13503, 13378, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13378, 12760, false),
            };

            vertices[13378] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(13378, new @Output(new @StageDirection("Freedom takes the figurine of a werewolf in the middle of an attack, mouth wide open.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13605, 13503, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13503, 13378, false),
            };

            vertices[13503] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(13503, new @Output(new @Line(Character.Freedom, " ", "ROAR! Take care, Mike, because I will *eat* you.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12760, 13605, true),
                new global::Phantonia.Historia.StoryEdge(14578, 13605, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13605, 13503, false),
            };

            vertices[13605] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(13605, new @Output(new @Line(Character.Mike, " ", "Careful, you're scaring the old man.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13953, 13765, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13765, 12760, false),
            };

            vertices[13765] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(13765, new @Output(new @StageDirection("Freedom takes a black bag and opens it. Inside there are a few golden looking coins each with a diameter of about 2cm. They keep them on their hand.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14032, 13953, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(13953, 13765, false),
            };

            vertices[13953] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(13953, new @Output(new @Line(Character.Freedom, " ", "How much are these worth?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14093, 14032, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14032, 13953, false),
            };

            vertices[14032] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(14032, new @Output(new @Line(Character.Mike, " ", "Worthless.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14156, 14093, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14093, 14032, false),
            };

            vertices[14093] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(14093, new @Output(new @Line(Character.Freedom, " ", "Very sad.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14373, 14156, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14156, 14093, false),
            };

            vertices[14156] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(14156, new @Output(new @Line(Character.Mike, " ", "They are props. But they're very good when you need to make a difficult choice. Look, this one has a happy and a frowny face on it. Or this one, with a cat and a dog.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14482, 14373, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14373, 14156, false),
            };

            vertices[14373] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(14373, new @Output(new @StageDirection("Freedom takes one of the coins and flips it. They look at the result.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(12760, 14482, true),
                new global::Phantonia.Historia.StoryEdge(14578, 14482, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14482, 14373, false),
            };

            vertices[14482] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(14482, new @Output(new @Line(Character.Freedom, " ", "Sad. Yeah, checks out, me too.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14646, 14578, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14578, 12760, false),
                new global::Phantonia.Historia.StoryEdge(14578, 13237, false),
                new global::Phantonia.Historia.StoryEdge(14578, 13605, false),
                new global::Phantonia.Historia.StoryEdge(14578, 14482, false),
            };

            vertices[14578] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(14578, new @Output(new @Line(Character.Mike, " ", "So, what do you choose?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            @Option[] options = { new @Option("Take the music box"), new @Option("Take the figurine"), new @Option("Take the bag of coins"), };

            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14827, 14646, false),
                new global::Phantonia.Historia.StoryEdge(15212, 14646, false),
                new global::Phantonia.Historia.StoryEdge(15750, 14646, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14646, 14578, false),
            };

            vertices[14646] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(14646, new @Output(new @Choice(Character.Freedom, Severity.Minor)), new global::Phantonia.Historia.ReadOnlyList<@Option>(options), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14913, 14827, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14827, 14646, false),
            };

            vertices[14827] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(14827, new @Output(new @StageDirection("Freedom takes the music box and stows it away.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15009, 14913, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(14913, 14827, false),
            };

            vertices[14913] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(14913, new @Output(new @Line(Character.Freedom, " ", "I'm taking the music box. I like sleeping.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16136, 15009, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15009, 14913, false),
            };

            vertices[15009] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(15009, new @Output(new @Line(Character.Mike, " ", "Very well. Have fun with that.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15297, 15212, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15212, 14646, false),
            };

            vertices[15212] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(15212, new @Output(new @StageDirection("Freedom takes the figurine and stows it away.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15386, 15297, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15297, 15212, false),
            };

            vertices[15297] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(15297, new @Output(new @Line(Character.Freedom, " ", "This is basically my spirit animal.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15465, 15386, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15386, 15297, false),
            };

            vertices[15386] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(15386, new @Output(new @Line(Character.Mike, " ", "Are werewolves even animals?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15561, 15465, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15465, 15386, false),
            };

            vertices[15465] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(15465, new @Output(new @Line(Character.Freedom, " ", "I don't know Mike, are werewolves animals?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16136, 15561, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15561, 15465, false),
            };

            vertices[15561] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(15561, new @Output(new @Line(Character.Mike, " ", "I don't know.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15839, 15750, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15750, 14646, false),
            };

            vertices[15750] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(15750, new @Output(new @StageDirection("Freedom takes the bag of coins and stows it away.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15968, 15839, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15839, 15750, false),
            };

            vertices[15839] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(15839, new @Output(new @Line(Character.Freedom, " ", "I am notoriously bad at making choices. Those will be vital to my survival.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16049, 15968, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(15968, 15839, false),
            };

            vertices[15968] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(15968, new @Output(new @Line(Character.Mike, " ", "But you just made this choice.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16136, 16049, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16049, 15968, false),
            };

            vertices[16049] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16049, new @Output(new @Line(Character.Freedom, " ", "I guess that is true.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16229, 16136, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16136, 15009, false),
                new global::Phantonia.Historia.StoryEdge(16136, 15561, false),
                new global::Phantonia.Historia.StoryEdge(16136, 16049, false),
            };

            vertices[16136] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16136, new @Output(new @Line(Character.Freedom, " ", "Thanks Mike. Not just for this, for everything.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16284, 16229, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16229, 16136, false),
            };

            vertices[16229] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16229, new @Output(new @Line(Character.Mike, " ", "Of course.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16356, 16284, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16284, 16229, false),
            };

            vertices[16284] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16284, new @Output(new @StageDirection("Freedom's eyes land on a pair of chests.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16432, 16356, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16356, 16284, false),
            };

            vertices[16356] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16356, new @Output(new @Line(Character.Freedom, " ", "These chests are quite pretty.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16527, 16432, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16432, 16356, false),
            };

            vertices[16432] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16432, new @Output(new @Line(Character.Mike, " ", "They aren't just pretty. They are quite handy. Look.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16695, 16527, false),
                new global::Phantonia.Historia.StoryEdge(17259, 16527, false),
                new global::Phantonia.Historia.StoryEdge(17819, 16527, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16527, 16432, false),
            };

            vertices[16527] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16527, new @Output(new @StageDirection("Mike takes the chests and hands Freedom one.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16773, 16695, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16695, 16527, false),
            };

            vertices[16695] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16695, new @Output(new @Line(Character.Mike, " ", "Put your music box in here.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16831, 16773, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16773, 16695, false),
            };

            vertices[16773] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16773, new @Output(new @Line(Character.Freedom, " ", "Why?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16891, 16831, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16831, 16773, false),
            };

            vertices[16831] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16831, new @Output(new @Line(Character.Mike, " ", "Trust me.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16975, 16891, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16891, 16831, false),
            };

            vertices[16891] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16891, new @Output(new @StageDirection("Freedom puts their music box into the chest.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17039, 16975, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(16975, 16891, false),
            };

            vertices[16975] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(16975, new @Output(new @Line(Character.Mike, " ", "Now close it.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18355, 17039, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17039, 16975, false),
            };

            vertices[17039] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(17039, new @Output(new @StageDirection("Freedom closes the lid of the chest. Mike opens his chest and pulls out the music box. When Freedom opens their chest, it is empty.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17336, 17259, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17259, 16527, false),
            };

            vertices[17259] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(17259, new @Output(new @Line(Character.Mike, " ", "Put your figurine in here.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17394, 17336, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17336, 17259, false),
            };

            vertices[17336] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(17336, new @Output(new @Line(Character.Freedom, " ", "Why?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17454, 17394, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17394, 17336, false),
            };

            vertices[17394] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(17394, new @Output(new @Line(Character.Mike, " ", "Trust me.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17537, 17454, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17454, 17394, false),
            };

            vertices[17454] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(17454, new @Output(new @StageDirection("Freedom puts their figurine into the chest.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17601, 17537, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17537, 17454, false),
            };

            vertices[17537] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(17537, new @Output(new @Line(Character.Mike, " ", "Now close it.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18355, 17601, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17601, 17537, false),
            };

            vertices[17601] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(17601, new @Output(new @StageDirection("Freedom closes the lid of the chest. Mike opens his chest and pulls out the figurine. When Freedom opens their chest, it is empty.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17900, 17819, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17819, 16527, false),
            };

            vertices[17819] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(17819, new @Output(new @Line(Character.Mike, " ", "Put your bag of coins in here.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17958, 17900, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17900, 17819, false),
            };

            vertices[17900] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(17900, new @Output(new @Line(Character.Freedom, " ", "Why?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18018, 17958, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(17958, 17900, false),
            };

            vertices[17958] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(17958, new @Output(new @Line(Character.Mike, " ", "Trust me.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18105, 18018, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18018, 17958, false),
            };

            vertices[18018] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(18018, new @Output(new @StageDirection("Freedom puts their bag of coins into the chest.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18169, 18105, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18105, 18018, false),
            };

            vertices[18105] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(18105, new @Output(new @Line(Character.Mike, " ", "Now close it.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18355, 18169, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18169, 18105, false),
            };

            vertices[18169] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(18169, new @Output(new @StageDirection("Freedom closes the lid of the chest. Mike opens his chest and pulls out the bag of coins. When Freedom opens their chest, it is empty.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18424, 18355, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18355, 17039, false),
                new global::Phantonia.Historia.StoryEdge(18355, 17601, false),
                new global::Phantonia.Historia.StoryEdge(18355, 18169, false),
            };

            vertices[18355] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(18355, new @Output(new @Line(Character.Freedom, " ", "Wow, that is very cool.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18530, 18424, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18424, 18355, false),
            };

            vertices[18424] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(18424, new @Output(new @Line(Character.Mike, " ", "It is, isn't it. And it works across any distance, just *poof*.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18647, 18530, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18530, 18424, false),
            };

            vertices[18530] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(18530, new @Output(new @Line(Character.Freedom, " ", "Over any distance, you say? I could use this to stay in touch with Cay.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18762, 18647, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18647, 18530, false),
            };

            vertices[18647] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(18647, new @Output(new @Line(Character.Mike, " ", "You could, yeah. Just put a letter in here, and Caylee will pull it out.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18863, 18762, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18762, 18647, false),
            };

            vertices[18762] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(18762, new @Output(new @Line(Character.Freedom, " ", "I've changed my mind. I will take these chests instead.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18982, 18863, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18863, 18762, false),
            };

            vertices[18863] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(18863, new @Output(new @Line(Character.Mike, " ", "Know what, I feel generous today. Take these and keep what you already have.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(19048, 18982, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(18982, 18863, false),
            };

            vertices[18982] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(18982, new @Output(new @Line(Character.Freedom, " ", "You're way too kind.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(19155, 19048, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(19048, 18982, false),
            };

            vertices[19048] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(19048, new @Output(new @Line(Character.Mike, " ", "I know, I know. Take care of yourself when you travel, will you?")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(19213, 19155, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(19155, 19048, false),
            };

            vertices[19155] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(19155, new @Output(new @Line(Character.Freedom, " ", "You know me.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(19289, 19213, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(19213, 19155, false),
            };

            vertices[19213] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(19213, new @Output(new @Line(Character.Mike, " ", "Yeah. That's why I'm saying this.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(19381, 19289, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(19289, 19213, false),
            };

            vertices[19289] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(19289, new @Output(new @Line(Character.Freedom, " ", "Wow okay. Yeah, I'm just leaving. Bye, Mike!")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        {
            global::Phantonia.Historia.StoryEdge[] outgoingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(-1, 19381, false),
            };

            global::Phantonia.Historia.StoryEdge[] incomingEdges =
            {
                new global::Phantonia.Historia.StoryEdge(19381, 19289, false),
            };

            vertices[19381] = new global::Phantonia.Historia.StoryVertex<@Output, @Option>(19381, new @Output(new @StageDirection("Freedom leaves Mike's stall and walks to the edge of the stage.")), new global::Phantonia.Historia.ReadOnlyList<@Option>(global::System.Array.Empty<@Option>()), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(outgoingEdges), new global::Phantonia.Historia.ReadOnlyList<Phantonia.Historia.StoryEdge>(incomingEdges));
        }

        global::Phantonia.Historia.StoryEdge[] startEdges = new global::Phantonia.Historia.StoryEdge[1];
        startEdges[0] = new global::Phantonia.Historia.StoryEdge(672, -2, false);
        return new global::Phantonia.Historia.StoryGraph<@Output, @Option>(vertices, startEdges);
    }
}
