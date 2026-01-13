using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoongladePure.Core.Utils;
namespace MoongladePure.Tests
{
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

            var text = string.Join("\n", p1, p2, p3, p4);
            
            // Max chunk size 1000
            var chunks = TextChunker.GetChunks(text, 1000).ToList();

            // Expecting 2 chunks
            // Chunk 1: p1 + p2 + p3 = 900 chars + newlines
            // Actually it splits by \n, so:
            // p1\n (301)
            // p2\n (301)
            // p3\n (301)
            // Total 903. 
            // Next is p4 (300). 903 + 300 = 1203 > 1000.
            // So chunk 1 should be p1\np2\np3 -> 903 chars (or 902 if trimmed at end)
            // Chunk 2 should be p4 -> 300 chars.

            // Chunk 1: p1 + p2 + p3 = 900 chars + newlines -> 300 + 1 + 300 + 1 + 300 = 902 (last \n trimmed)
            // Chunk 2: p4 = 300 chars
            
            // Replaced Assert.AreEqual(2, chunks.Count) with checking value.
            Assert.HasCount(2, chunks);
            
            Assert.AreEqual(902, chunks[0].Length);
            Assert.AreEqual(300, chunks[1].Length);
        }
    }
}
