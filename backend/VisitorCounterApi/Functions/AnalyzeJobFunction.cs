using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VisitorCounterApi.Analysis;
using VisitorCounterApi.Services;

namespace VisitorCounterApi.Functions;

public sealed record AnalyzeRequest(string? JobDescription);

// Only responsibility: receive the HTTP request, validate it, return the HTTP response.
public class AnalyzeJobFunction
{
    private const int MinLength = 80;
    private const int MaxLength = 5000;   // also stays within the size limit of one Azure AI Language document

    private readonly IJobMatchAnalyzer _analyzer;
    private readonly FixedWindowRateLimiter _limiter;
    private readonly ILogger<AnalyzeJobFunction> _logger;

    public AnalyzeJobFunction(
        IJobMatchAnalyzer analyzer,
        FixedWindowRateLimiter limiter,
        ILogger<AnalyzeJobFunction> logger)
    {
        _analyzer = analyzer;
        _limiter = limiter;
        _logger = logger;
    }

    [Function("AnalyzeJob")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "analyze")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!_limiter.TryAcquire())
        {
            return Error(StatusCodes.Status429TooManyRequests, "Zu viele Anfragen. Bitte in einigen Minuten erneut versuchen.");
        }

        if (req.ContentLength is > 20_000)
        {
            return Error(StatusCodes.Status413PayloadTooLarge, "Der Text ist zu lang.");
        }

        AnalyzeRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<AnalyzeRequest>(cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return Error(StatusCodes.Status400BadRequest, "Ungültige Anfrage.");
        }

        var text = body?.JobDescription?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length < MinLength)
        {
            return Error(StatusCodes.Status400BadRequest, $"Bitte mindestens {MinLength} Zeichen einfügen.");
        }

        if (text.Length > MaxLength)
        {
            return Error(StatusCodes.Status400BadRequest, $"Bitte höchstens {MaxLength} Zeichen einfügen.");
        }

        try
        {
            var result = await _analyzer.AnalyzeAsync(text, cancellationToken);

            if (result.Matched.Count + result.Missing.Count == 0)
            {
                return Error(StatusCodes.Status422UnprocessableEntity, "Es konnten keine Anforderungen erkannt werden.");
            }

            // The job text itself is never logged or stored.
            _logger.LogInformation(
                "Job match analysed. Length {Length}, score {Score}, source {Source}",
                text.Length, result.Score, result.KeywordSource);

            return new OkObjectResult(new
            {
                score = result.Score,
                grade = result.Grade,
                matched = result.Matched,
                missing = result.Missing,
                source = result.KeywordSource
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Job match analysis failed.");
            return Error(StatusCodes.Status500InternalServerError, "Die Analyse ist momentan nicht verfügbar.");
        }
    }

    private static ObjectResult Error(int statusCode, string message) =>
        new(new { error = message }) { StatusCode = statusCode };
}