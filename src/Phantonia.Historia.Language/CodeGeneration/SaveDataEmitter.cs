using Phantonia.Historia.Language.FlowAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis;
using Phantonia.Historia.Language.SemanticAnalysis.Symbols;
using Phantonia.Historia.Language.SyntaxAnalysis;
using Phantonia.Historia.Language.SyntaxAnalysis.Statements;
using System;
using System.CodeDom.Compiler;
using System.Linq;

namespace Phantonia.Historia.Language.CodeGeneration;

public sealed class SaveDataEmitter(StoryNode boundStory, SymbolTable symbolTable, Settings settings, IndentedTextWriter writer)
{
    // save data format v1
    // 1 byte: version (0x01 for now)
    // 8 bytes: fingerprint
    // 4 bytes: vertex
    // for each outcome: log(#options) bits
    // -> padded to full byte per outcome
    // for each spectrum: 8 bytes
    // for each tracker: log(#callsites) bits
    // -> padded to full byte per outcome
    // for each loop switch: 8 bytes
    // 1 byte: checksum
    public static int GetByteCount(StoryNode boundStory, SymbolTable symbolTable)
    {
        int byteCount = 14; // version + fingerprint + vertex + checksum

        foreach (OutcomeSymbol outcome in symbolTable.AllSymbols.OfType<OutcomeSymbol>())
        {
            if (outcome is SpectrumSymbol)
            {
                byteCount += 8;
            }
            else
            {
                byteCount += OptionCountToByteCount(outcome.OptionNames.Length);
            }
        }

        foreach (CallerTrackerSymbol tracker in symbolTable.AllSymbols.OfType<CallerTrackerSymbol>())
        {
            byteCount += OptionCountToByteCount(tracker.CallSiteCount);
        }

        byteCount += boundStory.FlattenHierarchie().OfType<LoopSwitchStatementNode>().Count() * 8;

        return byteCount;
    }


    public void GenerateGetSaveDataMethod()
    {
        writer.WriteLine("public static byte[] GetSaveData(Fields fields)");
        writer.BeginBlock();

        writer.Write("byte[] saveData = new byte[");
        writer.Write(GetByteCount(boundStory, symbolTable));
        writer.WriteLine("];");

        writer.WriteLine("saveData[0] = 0x01;");

        int i = 1;

        GenerateNumberSplitUp(() =>
        {
            writer.Write(settings.StoryName);
            writer.Write("Constants.Fingerprint");
        }, i, 8);
        i += 8;

        GenerateNumberSplitUp(() => writer.Write("fields.state"), i, 4);
        i += 4;

        GenerateOutcomeData(ref i);

        GenerateTrackerData(ref i);

        GenerateLoopSwitchData(ref i);

        GenerateChecksum(i);

        writer.WriteLine("return saveData;");

        writer.EndBlock(); // method
    }

    public void GenerateRestoreSaveDataMethod()
    {
        writer.WriteLine("public static bool TryRestoreSaveData(byte[] saveData, ref Fields fields)");
        writer.BeginBlock();

        writer.Write("if (saveData.Length != ");
        writer.Write(GetByteCount(boundStory, symbolTable));
        writer.Write(" || !global::Phantonia.Historia.SaveDataHelper.ValidateSaveData(saveData, ");
        writer.Write(settings.StoryName);
        writer.WriteLine("Constants.Fingerprint))");
        writer.BeginBlock();
        writer.WriteLine("return false;");
        writer.EndBlock(); // if

        writer.WriteLine();

        int i = 9;

        writer.Write("fields.state = ");
        GenerateNumberReconstruction("uint", i, 4);
        writer.WriteLine(';');
        i += 4;

        GenerateOutcomeRestoration(ref i);

        GenerateTrackerRestoration(ref i);

        GenerateLoopSwitchRestoration(ref i);

        writer.WriteLine();
        writer.WriteLine("return true;");

        writer.EndBlock(); // method
    }

    private void GenerateOutcomeData(ref int i)
    {
        foreach (OutcomeSymbol outcome in symbolTable.AllSymbols.OfType<OutcomeSymbol>())
        {
            if (outcome is SpectrumSymbol spectrum)
            {
                GenerateNumberSplitUp(() =>
                {
                    writer.Write("fields.");
                    GeneralEmission.GenerateSpectrumPositiveFieldName(spectrum, writer);
                }, i, 4);

                i += 4;

                GenerateNumberSplitUp(() =>
                {
                    writer.Write("fields.");
                    GeneralEmission.GenerateSpectrumTotalFieldName(spectrum, writer);
                },i, 4);

                i += 4;
            }
            else
            {
                int byteCount = OptionCountToByteCount(outcome.OptionNames.Length);

                GenerateNumberSplitUp(() =>
                {
                    writer.Write("fields.");
                    GeneralEmission.GenerateOutcomeFieldName(outcome, writer);
                }, i, byteCount);
                i += byteCount;
            }
        }
    }

    private void GenerateOutcomeRestoration(ref int i)
    {
        foreach (OutcomeSymbol outcome in symbolTable.AllSymbols.OfType<OutcomeSymbol>())
        {
            if (outcome is SpectrumSymbol spectrum)
            {
                writer.Write("fields.");
                GeneralEmission.GenerateSpectrumPositiveFieldName(spectrum, writer);
                writer.Write(" = ");
                GenerateNumberReconstruction("uint", i, 4);
                writer.WriteLine(';');

                i += 4;

                writer.Write("fields.");
                GeneralEmission.GenerateSpectrumTotalFieldName(spectrum, writer);
                writer.Write(" = ");
                GenerateNumberReconstruction("uint", i, 4);
                writer.WriteLine(';');

                i += 4;
            }
            else
            {
                int byteCount = OptionCountToByteCount(outcome.OptionNames.Length);

                writer.Write("fields.");
                GeneralEmission.GenerateOutcomeFieldName(outcome, writer);
                writer.Write(" = ");
                GenerateNumberReconstruction("uint", i, byteCount);
                writer.WriteLine(';');

                i += byteCount;
            }
        }
    }

    private void GenerateTrackerData(ref int i)
    {
        foreach (CallerTrackerSymbol tracker in symbolTable.AllSymbols.OfType<CallerTrackerSymbol>())
        {
            int byteCount = OptionCountToByteCount(tracker.CallSiteCount);

            GenerateNumberSplitUp(() =>
            {
                writer.Write("fields.");
                GeneralEmission.GenerateTrackerFieldName(tracker, writer);
            }, i, byteCount);
            i += byteCount;
        }
    }

    private void GenerateTrackerRestoration(ref int i)
    {
        foreach (CallerTrackerSymbol tracker in symbolTable.AllSymbols.OfType<CallerTrackerSymbol>())
        {
            int byteCount = OptionCountToByteCount(tracker.CallSiteCount);

            writer.Write("fields.");
            GeneralEmission.GenerateTrackerFieldName(tracker, writer);
            writer.Write(" = ");
            GenerateNumberReconstruction("uint", i, byteCount);
            writer.WriteLine(';');

            i += byteCount;
        }
    }

    private void GenerateLoopSwitchData(ref int i)
    {
        foreach (LoopSwitchStatementNode loopSwitch in boundStory.FlattenHierarchie().OfType<LoopSwitchStatementNode>())
        {
            GenerateNumberSplitUp(() =>
            {
                writer.Write("fields.");
                GeneralEmission.GenerateLoopSwitchFieldName(loopSwitch, writer);
            }, i, 8);
            i += 8;
        }
    }

    private void GenerateLoopSwitchRestoration(ref int i)
    {
        foreach (LoopSwitchStatementNode loopSwitch in boundStory.FlattenHierarchie().OfType<LoopSwitchStatementNode>())
        {
            writer.Write("fields.");
            GeneralEmission.GenerateLoopSwitchFieldName(loopSwitch, writer);
            writer.Write(" = ");
            GenerateNumberReconstruction("ulong", i, 8);
            writer.WriteLine(';');
            i += 8;
        }
    }

    private void GenerateChecksum(int i)
    {
        writer.WriteLine("unchecked");
        writer.BeginBlock();

        writer.Write("for (int i = 0; i < ");
        writer.Write(i);
        writer.WriteLine("; i++)");
        writer.BeginBlock();

        writer.Write("saveData[");
        writer.Write(i);
        writer.WriteLine("] += saveData[i];");

        writer.EndBlock(); // for

        writer.EndBlock(); // unchecked
    }

    private void GenerateNumberSplitUp(Action numberGenerator, int start, int count)
    {
        const ulong BitMask = (1ul << 8) - 1ul; // 0b11111111

        for (int i = 0; i < count; i++)
        {
            writer.Write("saveData[");
            writer.Write(start + i);
            writer.Write("] = (byte)((");
            numberGenerator();
            writer.Write(" & 0x");
            writer.Write((BitMask << (i * 8)).ToString("x"));
            writer.Write("ul) >> ");
            writer.Write(i * 8);
            writer.WriteLine(");");
        }
    }

    private void GenerateNumberReconstruction(string type, int start, int count)
    {
        writer.Write('(');
        writer.Write(type);
        writer.Write(")(");

        for (int i = 0; i < count; i++)
        {
            writer.Write("(saveData[");
            writer.Write(start + i);
            writer.Write("] << ");
            writer.Write(i * 8);
            writer.Write(')');

            if (i < count - 1)
            {
                writer.Write(" | ");
            }
        }

        writer.Write(')');
    }

    private static int OptionCountToByteCount(int optionCount)
    {
        int log = (int)Math.Ceiling(Math.Log2(optionCount));

        if (log % 8 == 0)
        {
            return log / 8;
        }
        else
        {
            return log / 8 + 1;
        }
    }
}
