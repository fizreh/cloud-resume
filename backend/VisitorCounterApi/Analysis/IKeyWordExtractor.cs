namespace VisitorCounterApi.Analysis;

public sealed record KeywordExtraction(IReadOnlyList<string> Phrases, string Source);

public interface IKeywordExtractor
{
    Task<KeywordExtraction> ExtractAsync(string text, CancellationToken cancellationToken = default);
}