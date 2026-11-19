using System.Net.Http.Json;
using System.Text.Json;
using VisitorCounterApi.Configuration;

namespace VisitorCounterApi.Analysis;

// Key phrase extraction with Azure AI Language (free tier).
public sealed class AzureLanguageKeywordExtractor : IKeywordExtractor
{
    private const string ApiVersion = "2023-04-01";

    private readonly HttpClient _httpClient;
    private readonly LanguageSettings _settings;

    public AzureLanguageKeywordExtractor(HttpClient httpClient, LanguageSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<KeywordExtraction> ExtractAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            throw new InvalidOperationException("Azure AI Language is not configured.");
        }

        var url = $"{_settings.Endpoint.TrimEnd('/')}/language/:analyze-text?api-version={ApiVersion}";

        var body = new
        {
            kind = "KeyPhraseExtraction",
            parameters = new { modelVersion = "latest" },
            analysisInput = new
            {
                documents = new[] { new { id = "1", language = LanguageGuess.Detect(text), text } }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("Ocp-Apim-Subscription-Key", _settings.Key);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var documents = json.RootElement.GetProperty("results").GetProperty("documents");
        if (documents.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Azure AI Language returned no document.");
        }

        var phrases = documents[0].GetProperty("keyPhrases")
            .EnumerateArray()
            .Select(element => element.GetString())
            .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
            .Select(phrase => phrase!)
            .ToList();

        return new KeywordExtraction(phrases, "Azure AI Language");
    }
}