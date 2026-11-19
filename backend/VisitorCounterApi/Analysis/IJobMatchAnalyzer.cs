namespace VisitorCounterApi.Analysis;

public interface IJobMatchAnalyzer
{
    Task<MatchResult> AnalyzeAsync(string jobDescription, CancellationToken cancellationToken = default);
}