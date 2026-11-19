using Microsoft.Extensions.Configuration;

namespace VisitorCounterApi.Configuration;

// A class (not a record), so a ToString() can never print the key into logs.
public sealed class LanguageSettings
{
    public string Endpoint { get; }
    public string Key { get; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(Key);

    private LanguageSettings(string endpoint, string key)
    {
        Endpoint = endpoint;
        Key = key;
    }

    // Optional on purpose: without these settings the analyser still works with the local fallback.
    public static LanguageSettings FromConfiguration(IConfiguration configuration) =>
        new(configuration[SettingNames.LanguageEndpoint] ?? "",
            configuration[SettingNames.LanguageKey] ?? "");
}