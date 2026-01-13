namespace MoongladePure.Core.Utils;

public static class TextChunker
{
    public static IEnumerable<string> GetChunks(string text, int maxChunkSize)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var paragraphs = text.Split('\n');
        var currentChunk = new StringBuilder();

        foreach (var paragraph in paragraphs) 
        {
            var p = paragraph + "\n"; // Add newline back as it was removed by Split
            
            // If adding the next paragraph exceeds the limit
            if (currentChunk.Length + p.Length > maxChunkSize)
            {
                // If the current chunk is not empty, yield it
                if (currentChunk.Length > 0)
                {
                    yield return currentChunk.ToString().TrimEnd();
                    currentChunk.Clear();
                }

                // If the paragraph itself is larger than the limit, yield it as a standalone chunk
                if (p.Length > maxChunkSize)
                {
                    yield return p.TrimEnd();
                }
                else
                {
                    // Otherwise, start the new chunk with this paragraph
                    currentChunk.Append(p);
                }
            }
            else
            {
                // Safe to add to current chunk
                currentChunk.Append(p);
            }
        }

        // Yield any remaining content
        if (currentChunk.Length > 0)
        {
            yield return currentChunk.ToString().TrimEnd();
        }
    }
}
