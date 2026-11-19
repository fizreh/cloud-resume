using Microsoft.Extensions.Logging;

namespace VisitorCounterApi.Analysis;

// Tries the primary extractor and falls back to the second one on any failure
// (network problem, wrong key, free quota used up).
public sealed class FallbackKeywordExtractor : IKeywordExtractor
{
    private readonly IKeywordExtractor _primary;
    private readonly IKeywordExtractor _fallback;
    private readonly ILogger<FallbackKeywordExtractor> _logger;

    public FallbackKeywordExtractor(
        IKeywordExtractor primary,
        IKeywordExtractor fallback,
        ILogger<FallbackKeywordExtractor> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<KeywordExtraction> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.ExtractAsync(text, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Log the reason only. Never log the job text.
            _logger.LogWarning("Primary keyword extraction failed ({Reason}). Using the local fallback.", ex.Message);
            return await _fallback.ExtractAsync(text, cancellationToken);
        }
    }
}