using System.Diagnostics;

HistoriaStoryStateMachine stateMachine = new();
stateMachine.TryContinue();

while (!stateMachine.FinishedStory)
{
    stateMachine.Output.Run(
        line =>
        {
            if (line.StageDirection!.Trim() != "")
            {
                Console.WriteLine($"{line.Character} ({line.StageDirection}): {line.Text}");
            }
            else
            {
                Console.WriteLine($"{line.Character} ({line.StageDirection}): {line.Text}");
            }

            Console.ReadKey();

            Debug.Assert(stateMachine.Options.Count == 0);
            stateMachine.TryContinue();
        },
        stageDirection =>
        {
            Console.WriteLine(stageDirection.Text);
            Console.ReadKey();

            Debug.Assert(stateMachine.Options.Count == 0);
            stateMachine.TryContinue();
        },
        choice =>
        {
            Debug.Assert(stateMachine.Options.Count > 0);

            for (int i = 0; i < stateMachine.Options.Count; i++)
            {
                Console.Write($"| [{i + 1}] {stateMachine.Options[i].Text} ");
            }

            Console.WriteLine("|");
            ConsoleKeyInfo key = Console.ReadKey();

            int option = key.KeyChar - '1';

            // throws if wrong char
            stateMachine.TryContinueWithOption(option);
        });
}