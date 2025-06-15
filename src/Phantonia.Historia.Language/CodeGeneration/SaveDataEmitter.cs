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
    // -> padded to full byte
    // for each spectrum: 16 bytes
    // for each tracker: log(#callsites) bits
    // for each loop switch: 8 bytes
    // 1 byte: checksum

    public void GenerateGetSaveDataMethod()
    {
        writer.WriteLine("public static byte[] GetSaveData(Fields fields)");
        writer.BeginBlock();

        writer.Write("byte[] saveData = new byte[");
        writer.Write(GetByteCount());
        writer.WriteLine("];");

        writer.WriteLine("saveData[0] = 0x01;");

        int i = 1;

        GenerateNumberSplitUp(() =>
        {
            writer.Write(settings.StoryName);
            writer.Write("Constants.Fingerprint");
        }, j => writer.Write(i + j), 8);
        i += 8;

        GenerateNumberSplitUp(() => writer.Write("fields.state"), j => writer.Write(i + j), 4);
        i += 4;

        GenerateOutcomeData(ref i);

        GenerateTrackerData(ref i);

        GenerateLoopSwitchData(ref i);

        GenerateChecksum(i);

        writer.WriteLine("return saveData;");

        writer.EndBlock(); // method
    }

    private int GetByteCount()
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
                int log = (int)Math.Ceiling(Math.Log2(outcome.OptionNames.Length));

                if (log % 8 == 0)
                {
                    byteCount += log / 8;
                }
                else
                {
                    byteCount += log / 8 + 1;
                }
            }
        }

        foreach (CallerTrackerSymbol tracker in symbolTable.AllSymbols.OfType<CallerTrackerSymbol>())
        {
            int log = (int)Math.Ceiling(Math.Log2(tracker.CallSiteCount));

            if (log % 8 == 0)
            {
                byteCount += log / 8;
            }
            else
            {
                byteCount += log / 8 + 1;
            }
        }

        byteCount += boundStory.FlattenHierarchie().OfType<LoopSwitchStatementNode>().Count() * 8;

        return byteCount;
    }

    private void GenerateOutcomeData(ref int i)
    {
        foreach (OutcomeSymbol outcome in symbolTable.AllSymbols.OfType<OutcomeSymbol>())
        {
            int iCopy = i;

            if (outcome is SpectrumSymbol spectrum)
            {
                GenerateNumberSplitUp(() =>
                {
                    writer.Write("fields.");
                    GeneralEmission.GenerateSpectrumPositiveFieldName(spectrum, writer);
                }, j => writer.Write(iCopy + j), 4);
                i += 4;

                iCopy = i;

                GenerateNumberSplitUp(() =>
                {
                    writer.Write("fields.");
                    GeneralEmission.GenerateSpectrumTotalFieldName(spectrum, writer);
                }, j => writer.Write(iCopy + j), 4);
                i += 4;
            }
            else
            {
                int log = (int)Math.Ceiling(Math.Log2(outcome.OptionNames.Length));
                int byteCount;

                if (log % 8 == 0)
                {
                    byteCount = log / 8;
                }
                else
                {
                    byteCount = log / 8 + 1;
                }

                GenerateNumberSplitUp(() =>
                {
                    writer.Write("fields.");
                    GeneralEmission.GenerateOutcomeFieldName(outcome, writer);
                }, j => writer.Write(iCopy + j), byteCount);
                i += byteCount;
            }
        }
    }

    private void GenerateTrackerData(ref int i)
    {
        foreach (CallerTrackerSymbol tracker in symbolTable.AllSymbols.OfType<CallerTrackerSymbol>())
        {
            int log = (int)Math.Ceiling(Math.Log2(tracker.CallSiteCount));
            int byteCount;

            if (log % 8 == 0)
            {
                byteCount = log / 8;
            }
            else
            {
                byteCount = log / 8 + 1;
            }

            int iCopy = i;

            GenerateNumberSplitUp(() =>
            {
                writer.Write("fields.");
                GeneralEmission.GenerateTrackerFieldName(tracker, writer);
            }, j => writer.Write(iCopy + j), byteCount);
            i += byteCount;
        }
    }

    private void GenerateLoopSwitchData(ref int i)
    {
        foreach (LoopSwitchStatementNode loopSwitch in boundStory.FlattenHierarchie().OfType<LoopSwitchStatementNode>())
        {
            int iCopy = i;

            GenerateNumberSplitUp(() =>
            {
                writer.Write("fields.");
                GeneralEmission.GenerateLoopSwitchFieldName(loopSwitch, writer);
            }, j => writer.Write(iCopy + j), 8);
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

    private void GenerateNumberSplitUp(Action numberGenerator, Action<int> indexGenerator, int count)
    {
        const ulong BitMask = (1ul << 8) - 1ul; // 0b11111111

        for (int i = 0; i < count; i++)
        {
            writer.Write("saveData[");
            indexGenerator(i);
            writer.Write("] = (byte)((");
            numberGenerator();
            writer.Write(" & 0x");
            writer.Write((BitMask << (i * 8)).ToString("x"));
            writer.Write("ul) >> ");
            writer.Write(i * 8);
            writer.WriteLine(");");
        }
    }
}
