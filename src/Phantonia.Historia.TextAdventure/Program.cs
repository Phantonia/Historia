using System;
using System.Diagnostics;

namespace Phantonia.Historia.TextAdventure;

internal class Program
{
    static void Main(string[] args)
    {
        HistoriaStory story = new();
        story.TryContinue();

        while (!story.FinishedStory)
        {
            story.Output.Run(
                line =>
                {
                    Console.WriteLine($"{line.Character}: {line.Text}");
                    _ = Console.ReadKey();
                    bool success = story.TryContinue();
                    Debug.Assert(success);
                },
                choice =>
                {
                    Console.WriteLine(choice.Prompt);

                    Debug.Assert(story.Options.Length > 0);

                    for (int i = 0; i < story.Options.Length; i++)
                    {
                        Console.WriteLine($"[{i}] {story.Options[i]}");
                    }

                    int userChoice;

                    do
                    {
                        Console.Write(">> ");
                    } while (!int.TryParse(Console.ReadLine(), out userChoice));

                    _ = Console.ReadKey();
                    bool success = story.TryContinueWithOption(userChoice);
                    Debug.Assert(success);
                });
        }
    }
}
