using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace Phantonia.Historia.Tests.Compiler;

internal static class ReflectionHelper
{
    public static object GetChapter(Assembly assembly, string storyName, string chapterName)
    {
        string chapterTypeName = $"{storyName}Chapter";
        Type? chapterType = assembly.GetType(chapterTypeName);

        string getMethodName = $"Chapter{chapterName}";
        MethodInfo? getMethod = chapterType?.GetMethod(getMethodName, BindingFlags.Static | BindingFlags.Public);

        object? chapterObject = getMethod?.Invoke(null, []);
        Assert.IsNotNull(chapterObject);
        return chapterObject;
    }

    public static void SetOutcome(object chapterObject, string outcomeName, int value)
    {
        Type? chapterType = chapterObject.GetType();

        string propertyName = $"Outcome{outcomeName}";
        PropertyInfo? outcomeProperty = chapterType?.GetProperty(propertyName);

        object? checkpointOutcome = outcomeProperty?.GetValue(chapterObject);
        Type? checkpointOutcomeType = checkpointOutcome?.GetType();
        MethodInfo? assignMethod = checkpointOutcomeType?.GetMethod("Assign");

        object? assignedOutcome = assignMethod?.Invoke(checkpointOutcome, [value]);
        outcomeProperty?.SetValue(chapterObject, assignedOutcome);
    }

    public static void SetSpectrum(object chapterObject, string spectrumName, uint positive, uint total)
    {
        Type? chapterType = chapterObject.GetType();

        string propertyName = $"Spectrum{spectrumName}";
        PropertyInfo? spectrumProperty = chapterType?.GetProperty(propertyName);

        object? checkpointSpectrum = spectrumProperty?.GetValue(chapterObject);
        Type? checkpointSpectrumType = checkpointSpectrum?.GetType();
        MethodInfo? assignMethod = checkpointSpectrumType?.GetMethod("Assign");

        object? assignedSpectrum = assignMethod?.Invoke(checkpointSpectrum, [positive, total]);
        spectrumProperty?.SetValue(chapterObject, assignedSpectrum);
    }

    public static bool IsReady(object chapterObject)
    {
        Type? chapterType = chapterObject.GetType();
        MethodInfo? isReadyMethod = chapterType?.GetMethod("IsReady");
        object? result = isReadyMethod?.Invoke(chapterObject, []);
        return (bool)result!;
    }

    public static void Restore<TOutput, TOption>(this IStoryStateMachine<TOutput, TOption> stateMachine, object chapter)
    {
        Type? stateMachineType = stateMachine?.GetType();
        MethodInfo? restoreMethod = stateMachineType?.GetMethod("RestoreChapter");
        restoreMethod?.Invoke(stateMachine, [chapter]);
    }

    public static int GetPublicOutcome<TOutput, TOption>(this IStoryStateMachine<TOutput, TOption> stateMachine, string outcomeName)
    {
        Type? stateMachineType = stateMachine?.GetType();
        PropertyInfo? property = stateMachineType?.GetProperty($"Outcome{outcomeName}");
        object? result = property?.GetValue(stateMachine);
        return (int)result!;
    }
}
