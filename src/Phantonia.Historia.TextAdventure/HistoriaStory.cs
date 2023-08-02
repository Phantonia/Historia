#nullable enable
using System;

public sealed class @HistoriaStory : global::Phantonia.Historia.IStory<@HistoriaStory.@Union, string?>
{
    public @HistoriaStory()
    {
        state = -2;
    }

    private int state;
    private int total216;
    private int positive216;

    public bool NotStartedStory { get; private set; } = true;

    public bool FinishedStory { get; private set; } = false;

    public global::System.Collections.Immutable.ImmutableArray<string?> Options { get; private set; } = global::System.Collections.Immutable.ImmutableArray<string?>.Empty;

    public @HistoriaStory.@Union Output { get; private set; }

    public bool TryContinue()
    {
        if (FinishedStory || Options.Length != 0)
        {
            return false;
        }

        StateTransition(0);
        Output = GetOutput();
        Options = GetOptions();

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
        if (FinishedStory || option < 0 || option >= Options.Length)
        {
            return false;
        }

        StateTransition(option);
        Output = GetOutput();
        Options = GetOptions();

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
            switch (state, option)
            {
                case (-2, _):
                    state = 282;
                    continue;
                case (282, _):
                    total216 += 2;
                    positive216 += 2;
                    state = 358;
                    return;
                case (358, _):
                    state = 444;
                    return;
                case (444, _):
                    state = 575;
                    return;
                case (575, _):
                    state = 666;
                    return;
                case (666, 0):
                    state = 777;
                    continue;
                case (666, 1):
                    state = 1805;
                    continue;
                case (777, _):
                    total216 += 3;
                    positive216 += 3;
                    state = 822;
                    return;
                case (822, _):
                    state = 904;
                    return;
                case (904, _):
                    state = 958;
                    return;
                case (958, 0):
                    state = 1090;
                    return;
                case (958, 1):
                    state = 1452;
                    continue;
                case (1090, _):
                    state = 1154;
                    return;
                case (1154, _):
                    state = 1216;
                    return;
                case (1216, _):
                    state = 1316;
                    return;
                case (1316, _):
                    state = 1667;
                    return;
                case (1452, _):
                    total216 += 1;
                    positive216 += 1;
                    state = 1505;
                    return;
                case (1505, _):
                    state = 1588;
                    return;
                case (1588, _):
                    state = 1667;
                    return;
                case (1667, _):
                    state = 2221;
                    return;
                case (1805, _):
                    total216 += 3;
                    state = 1846;
                    return;
                case (1846, _):
                    state = 1929;
                    return;
                case (1929, _):
                    state = 1993;
                    return;
                case (1993, _):
                    state = 2037;
                    return;
                case (2037, _):
                    state = 2091;
                    return;
                case (2091, _):
                    state = 2156;
                    return;
                case (2156, _):
                    state = 2221;
                    return;
                case (2221, _):
                    state = 2271;
                    return;
                case (2271, _):
                    state = 2400;
                    return;
                case (2400, _):
                    state = 2454;
                    return;
                case (2454, _):
                    state = 2549;
                    return;
                case (2549, 0):
                    state = 2655;
                    return;
                case (2549, 1):
                    state = 3934;
                    continue;
                case (2549, 2):
                    state = 4617;
                    continue;
                case (2655, _):
                    state = 2710;
                    return;
                case (2710, _):
                    state = 2754;
                    return;
                case (2754, 0):
                    state = 2904;
                    continue;
                case (2754, 1):
                    state = 3221;
                    continue;
                case (2754, 2):
                    state = 3542;
                    continue;
                case (2904, _):
                    total216 += 1;
                    state = 2951;
                    return;
                case (2951, _):
                    state = 3023;
                    return;
                case (3023, _):
                    state = 6046;
                    continue;
                case (3221, _):
                    total216 += 5;
                    state = 3268;
                    return;
                case (3268, _):
                    state = 3360;
                    return;
                case (3360, _):
                    state = 6046;
                    continue;
                case (3542, _):
                    total216 += 2;
                    positive216 += 2;
                    state = 3593;
                    return;
                case (3593, _):
                    state = 3687;
                    return;
                case (3687, _):
                    state = 3762;
                    return;
                case (3762, _):
                    state = 6046;
                    continue;
                case (3934, _):
                    total216 += 4;
                    positive216 += 4;
                    state = 3977;
                    return;
                case (3977, _):
                    state = 4069;
                    return;
                case (4069, _):
                    state = 4130;
                    return;
                case (4130, _):
                    state = 4220;
                    return;
                case (4220, _):
                    state = 4304;
                    return;
                case (4304, _):
                    state = 4350;
                    return;
                case (4350, _):
                    state = 4425;
                    return;
                case (4425, _):
                    state = 4475;
                    return;
                case (4475, _):
                    state = 6046;
                    continue;
                case (4617, _):
                    total216 += 5;
                    positive216 += 5;
                    state = 4660;
                    return;
                case (4660, _):
                    state = 4734;
                    return;
                case (4734, _):
                    state = 4790;
                    return;
                case (4790, _):
                    state = 4845;
                    return;
                case (4845, _):
                    state = 4965;
                    return;
                case (4965, 0):
                    state = 5116;
                    continue;
                case (4965, 1):
                    state = 5372;
                    continue;
                case (4965, 2):
                    state = 5788;
                    continue;
                case (5116, _):
                    total216 += 1;
                    positive216 += 1;
                    state = 5167;
                    return;
                case (5167, _):
                    state = 5235;
                    return;
                case (5235, _):
                    state = 6046;
                    continue;
                case (5372, _):
                    total216 += 3;
                    state = 5419;
                    return;
                case (5419, _):
                    state = 5493;
                    return;
                case (5493, _):
                    state = 5557;
                    return;
                case (5557, _):
                    state = 5632;
                    return;
                case (5632, _):
                    state = 6046;
                    continue;
                case (5788, _):
                    total216 += 3;
                    positive216 += 3;
                    state = 5839;
                    return;
                case (5839, _):
                    state = 5930;
                    return;
                case (5930, _):
                    state = 6046;
                    continue;
                case (6046, _):
                    {
                        int value = positive216 * 10;

                        if (value < total216 * 3)
                        {
                            state = 6121;
                            return;
                        }
                        else if (value <= total216 * 7)
                        {
                            state = 6265;
                            return;
                        }
                        else
                        {
                            state = 6438;
                            return;
                        }
                    }
                case (6121, _):
                    state = -1;
                    return;
                case (6265, _):
                    state = 6331;
                    return;
                case (6331, _):
                    state = -1;
                    return;
                case (6438, _):
                    state = 6510;
                    return;
                case (6510, _):
                    state = -1;
                    return;
            }

            throw new global::System.InvalidOperationException("Invalid state");
        }
    }

    private @HistoriaStory.@Union GetOutput()
    {
        switch (state)
        {
            case 358:
                return new @Union(new @Line("Mona", "Can I talk to you for a second? It's really important."));
            case 444:
                return new @Union(new @Line("Laura", "Honestly, I don't really have time right now. Need to finish this essay. Deadline is this evening."));
            case 575:
                return new @Union(new @Line("Mona", "Please, Laura. I don't exagerate when I say it's important."));
            case 666:
                return new @Union(new @Choice("What do you answer her?"));
            case 822:
                return new @Union(new @Line("Laura", "Okay. What's up, Mona? You sound concerned."));
            case 904:
                return new @Union(new @Line("Mona", "It's positive."));
            case 958:
                return new @Union(new @Choice("What do you answer her?"));
            case 1090:
                return new @Union(new @Line("Laura", "What is positive?"));
            case 1154:
                return new @Union(new @Line("Mona", "You know Darren?"));
            case 1216:
                return new @Union(new @Line("Laura", "Yeah, your complicated relationship status boyfriend?"));
            case 1316:
                return new @Union(new @Line("Mona", "I'm pregnant."));
            case 1505:
                return new @Union(new @Line("Laura", "I see. Are you pregnant from Darren?"));
            case 1588:
                return new @Union(new @Line("Mona", "Yeah."));
            case 1667:
                return new @Union(new @Line("Mona", "What am I gonna do?"));
            case 1846:
                return new @Union(new @Line("Laura", "I'm serious. Let's talk after this deadline."));
            case 1929:
                return new @Union(new @Line("Mona", "Fuck, Laura. I'm pregnant."));
            case 1993:
                return new @Union(new @Line("Laura", "What?"));
            case 2037:
                return new @Union(new @Line("Mona", "You know Darren?"));
            case 2091:
                return new @Union(new @Line("Laura", "You are pregnant from him?"));
            case 2156:
                return new @Union(new @Line("Mona", "What will I do?"));
            case 2221:
                return new @Union(new @Line("Laura", "It's gonna be okay."));
            case 2271:
                return new @Union(new @Line("Mona", "Really? I'm pregnant from an asshole. Also, I'm pregnant from the asshole I fucking love so much."));
            case 2400:
                return new @Union(new @Line("Laura", "Do you want to keep it?"));
            case 2454:
                return new @Union(new @Line("Mona", "No idea. I'm fucking 18. Should I really carry a fucking child?"));
            case 2549:
                return new @Union(new @Choice("What do you recommend Mona?"));
            case 2655:
                return new @Union(new @Line("Laura", "I'd say keep it."));
            case 2710:
                return new @Union(new @Line("Mona", "Why?"));
            case 2754:
                return new @Union(new @Choice("Why should Mona keep the baby?"));
            case 2951:
                return new @Union(new @Line("Laura", "You'll always regret this"));
            case 3023:
                return new @Union(new @Line("Mona", "I don't think I will. I have no connection to this blob of cells."));
            case 3268:
                return new @Union(new @Line("Laura", "It would be murder to kill this unborn child."));
            case 3360:
                return new @Union(new @Line("Mona", "What are you fucking on about? That's bullshit."));
            case 3593:
                return new @Union(new @Line("Laura", "It's what I would do. But make your own choice!"));
            case 3687:
                return new @Union(new @Line("Mona", "Thank you. I'm really unsure."));
            case 3762:
                return new @Union(new @Line("Laura", "You will figure it out. I'm here for you."));
            case 3977:
                return new @Union(new @Line("Laura", "You don't have to keep it if you think it's too much."));
            case 4069:
                return new @Union(new @Line("Mona", "I know. It's just hard."));
            case 4130:
                return new @Union(new @Line("Laura", "It always is. You still have enough time to decide."));
            case 4220:
                return new @Union(new @Line("Laura", "Just saying... I don't think I would keep it."));
            case 4304:
                return new @Union(new @Line("Mona", "Why not?"));
            case 4350:
                return new @Union(new @Line("Laura", "You have all your life ahead of you."));
            case 4425:
                return new @Union(new @Line("Mona", "That's true."));
            case 4475:
                return new @Union(new @Line("Laura", "You will figure it out. I'm here for you."));
            case 4660:
                return new @Union(new @Line("Laura", "That is not something I can answer."));
            case 4734:
                return new @Union(new @Line("Mona", "It's fucking hard."));
            case 4790:
                return new @Union(new @Line("Laura", "It is always is."));
            case 4845:
                return new @Union(new @Line("Mona", "I don't think it would be right to carry the baby. Not for me, and not for them."));
            case 4965:
                return new @Union(new @Choice("What do you answer her?"));
            case 5167:
                return new @Union(new @Line("Laura", "I think that's right."));
            case 5235:
                return new @Union(new @Line("Mona", "Thank you!"));
            case 5419:
                return new @Union(new @Line("Laura", "I don't think that's right."));
            case 5493:
                return new @Union(new @Line("Mona", "Oh suddenly. Okay."));
            case 5557:
                return new @Union(new @Line("Laura", "Still not my choice to make."));
            case 5632:
                return new @Union(new @Line("Mona", "I know."));
            case 5839:
                return new @Union(new @Line("Laura", "If you think that's the right choice, it is."));
            case 5930:
                return new @Union(new @Line("Mona", "Thank you. I really needed that."));
            case 6121:
                return new @Union(new @Line("Mona", "Honestly, I regret ever talking to you about this. My god."));
            case 6265:
                return new @Union(new @Line("Mona", "Thanks for listening, Laura."));
            case 6331:
                return new @Union(new @Line("Laura", "You're always welcome."));
            case 6438:
                return new @Union(new @Line("Mona", "Thanks so much. I love you, Laura."));
            case 6510:
                return new @Union(new @Line("Laura", "Love you so much."));
            case -1:
                return default;
        }

        throw new global::System.InvalidOperationException("Invalid state");
    }

    private global::System.Collections.Immutable.ImmutableArray<string?> GetOptions()
    {
        switch (state)
        {
            case 666:
                return global::System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { "Fine. What's up?", "Let's talk after the deadline, okay?", });
            case 958:
                return global::System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { "What?", "I see...", });
            case 2549:
                return global::System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { "Keep it", "Don't keep it", "Only you can know", });
            case 2754:
                return global::System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { "You'll regret it", "It would be murder", "It's what I would do", });
            case 4965:
                return global::System.Collections.Immutable.ImmutableArray.ToImmutableArray(new[] { "That's the right choice.", "That's wrong", "It's right if you think it's right", });
        }

        return global::System.Collections.Immutable.ImmutableArray<string?>.Empty;
    }

    public readonly struct @Line : global::System.IEquatable<@Line>
    {
        internal @Line(string? @Character, string? @Text)
        {
            this.@Character = @Character;
            this.@Text = @Text;
        }

        public string? @Character { get; }

        public string? @Text { get; }

        public bool Equals(@Line other)
        {
            return @Character == other.@Character && @Text == other.@Text;
        }

        public override bool Equals(object? other)
        {
            return other is @Line record && Equals(record);
        }

        public override int GetHashCode()
        {
            global::System.HashCode hashcode = default;
            hashcode.Add(@Character);
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
    public readonly struct @Choice : global::System.IEquatable<@Choice>
    {
        internal @Choice(string? @Prompt)
        {
            this.@Prompt = @Prompt;
        }

        public string? @Prompt { get; }

        public bool Equals(@Choice other)
        {
            return @Prompt == other.@Prompt;
        }

        public override bool Equals(object? other)
        {
            return other is @Choice record && Equals(record);
        }

        public override int GetHashCode()
        {
            global::System.HashCode hashcode = default;
            hashcode.Add(@Prompt);
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
    public readonly struct @Union : global::System.IEquatable<@Union>, global::Phantonia.Historia.IUnion<@HistoriaStory.@Line, @HistoriaStory.@Choice>
    {
        internal @Union(@HistoriaStory.@Line value)
        {
            this.@Line = value;
            Discriminator = UnionDiscriminator.@Line;
        }

        internal @Union(@HistoriaStory.@Choice value)
        {
            this.@Choice = value;
            Discriminator = UnionDiscriminator.@Choice;
        }

        public @HistoriaStory.@Line @Line { get; }

        public @HistoriaStory.@Choice @Choice { get; }

        public UnionDiscriminator Discriminator { get; }

        public object? AsObject()
        {
            switch (Discriminator)
            {
                case UnionDiscriminator.@Line:
                    return this.@Line;
                case UnionDiscriminator.@Choice:
                    return this.@Choice;
            }

            throw new global::System.InvalidOperationException("Invalid discriminator");
        }

        public void Run(global::System.Action<@HistoriaStory.@Line> actionLine, global::System.Action<@HistoriaStory.@Choice> actionChoice)
        {
            switch (Discriminator)
            {
                case UnionDiscriminator.@Line:
                    actionLine(this.@Line);
                    return;
                case UnionDiscriminator.@Choice:
                    actionChoice(this.@Choice);
                    return;
            }

            throw new global::System.InvalidOperationException("Invalid discriminator");
        }

        public T Evaluate<T>(global::System.Func<@HistoriaStory.@Line, T> functionLine, global::System.Func<@HistoriaStory.@Choice, T> functionChoice)
        {
            switch (Discriminator)
            {
                case UnionDiscriminator.@Line:
                    return functionLine(this.@Line);
                case UnionDiscriminator.@Choice:
                    return functionChoice(this.@Choice);
            }

            throw new global::System.InvalidOperationException("Invalid discriminator");
        }

        public bool Equals(@Union other)
        {
            return Discriminator == other.Discriminator && this.@Line == other.@Line && this.@Choice == other.@Choice;
        }

        public override bool Equals(object? other)
        {
            return other is @Union union && Equals(union);
        }

        public override int GetHashCode()
        {
            global::System.HashCode hashcode = default;
            hashcode.Add(this.@Line);
            hashcode.Add(this.@Choice);
            return hashcode.ToHashCode();
        }

        public static bool operator ==(@Union x, @Union y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(@Union x, @Union y)
        {
            return !x.Equals(y);
        }

        public enum UnionDiscriminator
        {
            @Line,
            @Choice,
        }

        @HistoriaStory.@Line global::Phantonia.Historia.IUnion<@HistoriaStory.@Line, @HistoriaStory.@Choice>.Value0
        {
            get
            {
                return this.@Line;
            }
        }

        @HistoriaStory.@Choice global::Phantonia.Historia.IUnion<@HistoriaStory.@Line, @HistoriaStory.@Choice>.Value1
        {
            get
            {
                return this.@Choice;
            }
        }

        int global::Phantonia.Historia.IUnion<@HistoriaStory.@Line, @HistoriaStory.@Choice>.Discriminator
        {
            get
            {
                return (int)Discriminator;
            }
        }
    }

    global::System.Collections.Generic.IReadOnlyList<string?> global::Phantonia.Historia.IStory<@HistoriaStory.@Union, string?>.Options
    {
        get
        {
            return Options;
        }
    }
}

