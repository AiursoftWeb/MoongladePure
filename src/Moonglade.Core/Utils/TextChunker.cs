using System.Text.RegularExpressions;

namespace MoongladePure.Core.Utils;

public record TranslationChunk(string Content, bool IsCodeBlock);

public static class TextChunker
{
    public static IEnumerable<TranslationChunk> GetChunks(string text, int maxChunkSize)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        // 1. Identify code blocks
        var codeBlockRegex = new Regex(@"^```[\s\S]*?^```", RegexOptions.Multiline | RegexOptions.Compiled);
        var matches = codeBlockRegex.Matches(text);

        var segments = new List<TranslationChunk>();
        int lastIndex = 0;

        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                var textBefore = text.Substring(lastIndex, match.Index - lastIndex);
                AddParagraphs(segments, textBefore);
            }

            segments.Add(new TranslationChunk(match.Value, true));
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            var textAfter = text.Substring(lastIndex);
            AddParagraphs(segments, textAfter);
        }

        // 2. Greedy combine
        var currentChunkContent = new StringBuilder();
        foreach (var segment in segments)
        {
            if (segment.IsCodeBlock)
            {
                if (currentChunkContent.Length > 0)
                {
                    yield return new TranslationChunk(currentChunkContent.ToString().Trim(), false);
                    currentChunkContent.Clear();
                }
                yield return segment;
            }
            else
            {
                if (currentChunkContent.Length > 0 && currentChunkContent.Length + segment.Content.Length + 2 > maxChunkSize)
                {
                    yield return new TranslationChunk(currentChunkContent.ToString().Trim(), false);
                    currentChunkContent.Clear();
                }

                if (segment.Content.Length > maxChunkSize)
                {
                    if (currentChunkContent.Length > 0)
                    {
                        yield return new TranslationChunk(currentChunkContent.ToString().Trim(), false);
                        currentChunkContent.Clear();
                    }
                    yield return segment;
                }
                else
                {
                    if (currentChunkContent.Length > 0)
                    {
                        currentChunkContent.Append("\n\n");
                    }
                    currentChunkContent.Append(segment.Content);
                }
            }
        }

        if (currentChunkContent.Length > 0)
        {
            yield return new TranslationChunk(currentChunkContent.ToString().Trim(), false);
        }
    }

    private static void AddParagraphs(List<TranslationChunk> segments, string text)
    {
        var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in paragraphs)
        {
            var trimmed = p.Trim('\r', '\n');
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                segments.Add(new TranslationChunk(trimmed, false));
            }
        }
    }
}