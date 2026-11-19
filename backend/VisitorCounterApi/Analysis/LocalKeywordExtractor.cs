using System.Text.RegularExpressions;

namespace VisitorCounterApi.Analysis;

// Fallback without any cloud service: the most frequent meaningful words.
public sealed class LocalKeywordExtractor : IKeywordExtractor
{
    private const int MaxKeywords = 25;
    private static readonly Regex WordPattern = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

    public Task<KeywordExtraction> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        var keywords = WordPattern.Matches(TextNormalizer.Normalize(text))
            .Select(match => match.Value)
            .Where(word => word.Length > 3 && !TextNormalizer.IsStopWord(word))
            .GroupBy(TextNormalizer.Stem)
            .OrderByDescending(group => group.Count())
            .Take(MaxKeywords)
            .Select(group => group.First())
            .ToList();

        return Task.FromResult(new KeywordExtraction(keywords, "Lokale Erkennung"));
    }
}