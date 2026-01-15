using MoongladePure.Core.Utils;

namespace MoongladePure.Tests;

[TestClass]
public class TextChunkerTests
{
    [TestMethod]
    public void TestChunkingLogic()
    {
        // Prepare 4 paragraphs, each 300 chars long
        var p1 = new string('a', 300);
        var p2 = new string('b', 300);
        var p3 = new string('c', 300);
        var p4 = new string('d', 300);

        var text = string.Join("\n\n", p1, p2, p3, p4);

        // Max chunk size 1000
        var chunks = TextChunker.GetChunks(text, 1000).ToList();

        // Expecting 2 chunks
        // Chunk 1: p1 + p2 + p3 = 900 chars + 4 chars for \n\n separators = 904
        // Chunk 2: p4 = 300 chars

        Assert.HasCount(2, chunks);

        Assert.IsFalse(chunks[0].IsCodeBlock);
        Assert.AreEqual(904, chunks[0].Content.Length);
        
        Assert.IsFalse(chunks[1].IsCodeBlock);
        Assert.AreEqual(300, chunks[1].Content.Length);
    }

    [TestMethod]
    public void TestCodeBlockProtection()
    {
        var text = """
                   P1 before code.

                   ```csharp
                   var x = 1;
                   var y = 2;
                   ```

                   P2 after code.
                   """;

        var chunks = TextChunker.GetChunks(text, 1000).ToList();

        // Should have 3 chunks
        Assert.HasCount(3, chunks);

        Assert.IsFalse(chunks[0].IsCodeBlock);
        Assert.AreEqual("P1 before code.", chunks[0].Content);

        Assert.IsTrue(chunks[1].IsCodeBlock);
        Assert.IsTrue(chunks[1].Content.Contains("var x = 1;"));

        Assert.IsFalse(chunks[2].IsCodeBlock);
        Assert.AreEqual("P2 after code.", chunks[2].Content);
    }

    [TestMethod]
    public void TestLargeParagraph()
    {
        var p1 = new string('a', 1200);
        var text = p1;

        var chunks = TextChunker.GetChunks(text, 1000).ToList();

        Assert.HasCount(1, chunks);
        Assert.AreEqual(1200, chunks[0].Content.Length);
    }

    [TestMethod]
    public void TestGreedyWithCodeBlock()
    {
        var p1 = "P1";
        var p2 = "P2";
        var code = "```\ncode\n```";
        var p3 = "P3";

        var text = $"{p1}\n\n{p2}\n\n{code}\n\n{p3}";

        var chunks = TextChunker.GetChunks(text, 1000).ToList();

        // P1 and P2 should be combined
        // code should be separate
        // P3 should be separate

        Assert.HasCount(3, chunks);
        Assert.AreEqual("P1\n\nP2", chunks[0].Content);
        Assert.IsTrue(chunks[1].IsCodeBlock);
        Assert.AreEqual("P3", chunks[2].Content);
    }
}