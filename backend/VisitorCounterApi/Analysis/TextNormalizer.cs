using System.Text.RegularExpressions;

namespace VisitorCounterApi.Analysis;

// Turns free text into comparable tokens (lower case, tech names kept intact, light stemming for DE/EN).
public static class TextNormalizer
{
    // Technology names contain characters that a normal tokenizer would cut apart.
    private static readonly (string From, string To)[] Replacements =
    {
        ("c#", "csharp"), ("c++", "cplusplus"), ("f#", "fsharp"),
        ("asp.net", "aspnet"), (".net", "dotnet"),
        ("node.js", "nodejs"), ("next.js", "nextjs"), ("vue.js", "vuejs"),
        ("ci/cd", "cicd"), ("ci-cd", "cicd"),
    };

    private static readonly string[] Suffixes = { "ungen", "ung", "ern", "en", "er", "es", "e", "n", "s" };

    private static readonly Regex TokenPattern = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        // German
        "und", "oder", "der", "die", "das", "den", "dem", "des", "ein", "eine", "einer", "einen", "einem",
        "mit", "für", "von", "zu", "zum", "zur", "im", "in", "an", "auf", "bei", "als", "auch", "sowie",
        "sind", "ist", "sein", "wir", "sie", "ihr", "ihre", "unser", "unsere", "du", "dein", "deine",
        "erfahrung", "kenntnisse", "bereich", "team", "stelle", "werden", "wird", "haben", "hast",
        "nach", "über", "aus", "um", "nicht", "mehr", "sehr", "gute", "guten",
        // English
        "and", "or", "the", "a", "an", "of", "to", "for", "with", "on", "at", "as", "is", "are", "be",
        "we", "you", "your", "our", "will", "have", "has", "experience", "knowledge", "work", "working",
        "strong", "good", "ability", "skills"
    };

    public static string Normalize(string text)
    {
        var result = text.ToLowerInvariant();
        foreach (var (from, to) in Replacements)
        {
            result = result.Replace(from, to, StringComparison.Ordinal);
        }
        return result;
    }

    public static bool IsStopWord(string word) => StopWords.Contains(word);

    // Meaningful, stemmed tokens of a text.
    public static IReadOnlyList<string> ContentTokens(string text) =>
        TokenPattern.Matches(Normalize(text))
            .Select(match => match.Value)
            .Where(token => token.Length > 1 && !StopWords.Contains(token))
            .Select(Stem)
            .ToList();

    // Very light stemming (Entwickler/Entwicklung, developer/developers). The same rule is applied
    // to the CV and the job text, so both sides always compare like with like.
    public static string Stem(string token)
    {
        if (token.Length <= 5)
        {
            return token;
        }

        foreach (var suffix in Suffixes)
        {
            if (token.EndsWith(suffix, StringComparison.Ordinal) && token.Length - suffix.Length >= 4)
            {
                return token[..^suffix.Length];
            }
        }

        return token;
    }
}