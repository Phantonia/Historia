using Microsoft.Win32;
using Phantonia.Historia.Language;
using System;
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
        buttonBuild.Click += OnBuild;
        buttonContinue.Click += OnContinue;
    }

    private string? openedFile;
    private InterpreterStateMachine? stateMachine;

    private void OnBuild(object sender, RoutedEventArgs e)
    {
        TextReader GetInputReader()
        {
            if (openedFile is not null)
            {
                return new StreamReader(openedFile);
            }

            return new StringReader(textboxFile.Text);
        }

        using TextReader input = GetInputReader();

        Interpreter intp = new(input);
        InterpretationResult result = intp.Interpret();

        if (!result.IsValid)
        {
            string fullCode = GetInputReader().ReadToEnd();
            StringBuilder builder = new();

            foreach (Error error in result.Errors)
            {
                string errorMessage = Errors.GenerateFullMessage(fullCode, error);

                builder.AppendLine(errorMessage);
                builder.AppendLine();
            }

            textboxConsole.Text = builder.ToString();
            buttonContinue.IsEnabled = true;
        }
        else
        {
            stateMachine = result.StateMachine;

            textboxConsole.Text = "Build successful";
        }
    }

    private void OnContinue(object sender, RoutedEventArgs e)
    {
        if (stateMachine is null || stateMachine.Options.Count > 0)
        {
            buttonContinue.IsEnabled = false;
            return;
        }

        stateMachine.TryContinue();

        UpdateState();
    }

    private void UpdateState()
    {
        if (stateMachine is null)
        {
            return;
        }

        void UpdateTreeViewItem(TreeViewItem item, object? value)
        {
            if (value is not RecordInstance recordInstance)
            {
                item.Header = value?.ToString() ?? "<null>";
                return;
            }

            item.Header = recordInstance.RecordName;

            foreach ((string propertyName, object propertyValue) in recordInstance.Properties)
            {
                TreeViewItem newItem = new()
                {
                    Header = propertyName,
                };
                item.Items.Add(newItem);

                TreeViewItem newNewItem = new();
                UpdateTreeViewItem(newNewItem, propertyValue);
                newItem.Items.Add(newNewItem);
            }
        }

        {
            tviOutput.Items.Clear();
            TreeViewItem newItem = new();
            UpdateTreeViewItem(newItem, stateMachine.Output);
            tviOutput.Items.Add(newItem);
        }

        tviOptions.Items.Clear();

        foreach (object option in stateMachine.Options)
        {
            TreeViewItem newItem = new();
            UpdateTreeViewItem(newItem, option);
            tviOptions.Items.Add(newItem);
        }

        if (stateMachine.Options.Count > 0)
        {
            buttonContinue.IsEnabled = false;

            for (int i = 0; i < stateMachine.Options.Count; i++)
            {
                Button optionButton = new()
                {
                    Content = $"Option {i}",
                };

                int index = i;

                optionButton.Click += (_, _) =>
                {
                    stateMachine.TryContinueWithOption(index);
                    UpdateState();
                };

                stackpanelButtons.Children.Add(optionButton);
            }
        }
        else
        {
            buttonContinue.IsEnabled = true;

            for (int i = 1; i < stackpanelButtons.Children.Count; i++)
            {
                stackpanelButtons.Children.RemoveAt(i);
            }
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
            textboxFile.IsReadOnly = true;

            labelCurrentFile.Content = "Open file: " + openedFile;
        }
    }

    private void OnExport(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
        //if (outputCode is null)
        //{
        //    return;
        //}

        //SaveFileDialog dlg = new()
        //{
        //    Filter = "C# files (*.cs)|*.cs",
        //};

        //bool? result = dlg.ShowDialog(this);

        //if (result == true)
        //{
        //    using StreamWriter writer = new(dlg.FileName);
        //    writer.Write(outputCode);
        //}
    }
}
