using Microsoft.Win32;
using Phantonia.Historia.Language;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Phantonia.Historia.Studio;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        menuItemOpenFile.Click += OnOpenFile;
        menuItemExport.Click += OnExport;
        buttonCompile.Click += OnCompile;
        buttonContinue.Click += OnContinue;
    }

    private string? openedFile;
    private string? outputCode;
    private IStory? story;

    private void OnCompile(object sender, RoutedEventArgs e)
    {
        if (openedFile is null)
        {
            return;
        }

        using StreamReader input = new(openedFile);
        using StringWriter output = new();

        Compiler compiler = new(input, output);
        CompilationResult compilationResult = compiler.Compile();

        if (!compilationResult.IsValid)
        {
            string fullCode = File.ReadAllText(openedFile);
            StringBuilder builder = new();

            foreach (Error error in compilationResult.Errors)
            {
                string errorMessage = Errors.GenerateFullMessage(fullCode, error);

                builder.AppendLine(errorMessage);
                builder.AppendLine();
            }

            textboxConsole.Text = builder.ToString();
        }
        else
        {
            Debug.Assert(compilationResult.StoryName is not null);
            outputCode = output.ToString();
            story = DynamicCompiler.CompileToStory(outputCode, compilationResult.StoryName);
        }
    }

    private void OnContinue(object sender, RoutedEventArgs e)
    {
        if (story is null)
        {
            return;
        }

        if (story.Options.Count == 0)
        {
            story.TryContinue();
        }
        else
        {
            int option = comboboxOptions.SelectedIndex;
            story.TryContinueWithOption(option);
        }

        textboxOutput.Text = story.Output?.ToString() ?? "";

        comboboxOptions.Items.Clear();

        foreach (object? option in story.Options)
        {
            ComboBoxItem item = new()
            {
                Content = option?.ToString() ?? "",
            };

            comboboxOptions.Items.Add(item);
        }
    }

    private void OnOpenFile(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dlg = new()
        {
            CheckFileExists = true,
            Filter = "Historia scripts (*.hstr)|*.hstr",
        };

        bool? result = dlg.ShowDialog(this);

        if (result == true)
        {
            openedFile = dlg.FileName;

            using StreamReader stream = new(openedFile);
            textboxFile.Text = stream.ReadToEnd();

            labelCurrentFile.Content = "Open file: " + openedFile;
        }
    }

    private void OnExport(object sender, RoutedEventArgs e)
    {
        if (outputCode is null)
        {
            return;
        }

        SaveFileDialog dlg = new()
        {
            Filter = "C# files (*.cs)|*.cs",
        };

        bool? result = dlg.ShowDialog(this);

        if (result == true)
        {
            using StreamWriter writer = new(dlg.FileName);
            writer.Write(outputCode);
        }
    }
}
