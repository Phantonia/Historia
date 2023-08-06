using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Phantonia.Historia.Tests.Compiler;

internal static class DynamicCompiler
{
    public static IStory CompileToStory(string csharpCode)
    {
        // define source code, then parse it (to the type used for compilation)
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);

        string runtimePath = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll");

        // define other necessary objects for compilation
        string assemblyName = Path.GetRandomFileName();
        MetadataReference[] references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(runtimePath),
            MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IStory).Assembly.Location),
        };

        // analyse and generate IL code from syntax tree
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using MemoryStream ms = new();

        // write IL code into memory
        EmitResult result = compilation.Emit(ms);

        if (!result.Success)
        {
            // handle exceptions
            IEnumerable<Diagnostic> failures = result.Diagnostics;

            foreach (Diagnostic diagnostic in failures)
            {
                Debug.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
            }

            Assert.Fail($"C# code has errors:\n{string.Join("\n", failures.Select(f => f.GetMessage()))}");
            return null;
        }
        else
        {
            // load this 'virtual' DLL so that we can use
            ms.Seek(0, SeekOrigin.Begin);
            Assembly assembly = Assembly.Load(ms.ToArray());

            // create instance of the desired class and call the desired function
            Type? type = assembly.GetType("HistoriaStory");
            Assert.IsNotNull(type);

            object? obj = Activator.CreateInstance(type);
            Assert.IsNotNull(obj);

            return (IStory)obj;
        }
    }

    public static IStory<TOutput, TOption> CompileToStory<TOutput, TOption>(string csharpCode)
    {
        IStory story = CompileToStory(csharpCode);

        Assert.IsTrue(story is IStory<TOutput, TOption>);

        return (IStory<TOutput, TOption>)story;
    }
}
