using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantonia.Historia.Language;

namespace Phantonia.Historia.Tests.Compiler;

[TestClass]
public sealed class FingerprintingTests
{
    private ulong GetFingerprint(string code)
    {
        (CompilationResult result, _) = Language.Compiler.CompileString(code);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Length);

        return result.Fingerprint;
    }

    [TestMethod]
    public void TestFingerprintPersistence()
    {
        // fingerprints are guaranteed to stay the same under the following edits:
        // * renaming a symbol
        // * adding or removing output/ line statements
        // * changing any expression
        // * adding or removing an argument name
        // * adding or removing whitespace
        // * adding or removing outcome assignment or spectrum adjustment statements

        string v1 =
            """
            line record Line(Character: String, Text: String);
            union Output(Int, Line);

            setting OutputType: Output;

            outcome X(A, B);

            interface I
            (
                action A(x: Int),
            );

            reference R: I;

            chapter main
            {
                output 0;

                run R.A(294);
            }
            """;

        string v2 =
            """
            // messed with whitespace
            line record Line(Character:String,Text:String);
            union Output(Int,Line);
            setting OutputType:Output;
            outcome Y(A,B); // renamed
            interface J(action A(x:Int)); // removed trailing comma
            reference S:J;
            
            chapter main
            {
                // removed output statement
                run S.A(x = 7); // changed expression and added argument name
                
                "Alice": "Hello World"; // added line statement
                Y = B; // added outcome assignment

                run S.A(819); // added run statement
            }
            """;

        ulong fingerprint1 = GetFingerprint(v1);
        ulong fingerprint2 = GetFingerprint(v2);

        Assert.AreEqual(fingerprint1, fingerprint2);
    }

    // TODO: test more
}
