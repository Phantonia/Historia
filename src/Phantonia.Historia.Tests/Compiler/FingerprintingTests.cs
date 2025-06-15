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
        // * changing any expression
        // * adding or removing an argument name
        // * adding or removing whitespace/comments

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
            outcome X(A,B);
            interface I(action A(x:Int)); // removed trailing comma
            reference R:I;
            
            chapter main
            {
                output 13; // changed expression
                run R.A(x = 7); // changed expression and added argument name
            }
            """;

        ulong fingerprint1 = GetFingerprint(v1);
        ulong fingerprint2 = GetFingerprint(v2);

        // got this from compiling once
        // we're testing that it stays the same for future versions of the compiler
        const ulong ExpectedFingerprint = 0x1bb8da4018cfd717;

        Assert.AreEqual(ExpectedFingerprint, fingerprint1);
        Assert.AreEqual(ExpectedFingerprint, fingerprint2);
    }

    // TODO: test more
}
