using System.Text.RegularExpressions;

namespace VisitorCounterApi.Analysis;

// Guesses "de" or "en" so Azure AI Language gets the right language hint.
public static class LanguageGuess
{
    private static readonly HashSet<string> German = new()
    {
        "und", "der", "die", "das", "mit", "für", "wir", "sie", "ein", "eine", "von", "zu", "im", "auf", "ist", "sind"
    };

    private static readonly HashSet<string> English = new()
    {
        "and", "the", "with", "for", "you", "we", "of", "to", "in", "is", "are", "our", "will"
    };

    public static string Detect(string text)
    {
        var words = Regex.Matches(text.ToLowerInvariant(), @"\p{L}+").Select(match => match.Value).ToList();
        var german = words.Count(German.Contains);
        var english = words.Count(English.Contains);
        return german >= english ? "de" : "en";
    }
}