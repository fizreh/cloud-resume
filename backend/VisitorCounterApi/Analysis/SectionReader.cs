using System.Text.RegularExpressions;

namespace VisitorCounterApi.Analysis;

// One sentence or line of the job text with its importance factor.
public sealed record JobLine(ISet<string> Tokens, double Factor);

// Splits the job text into lines and weights them:
// "must have" sections count more, "nice to have" parts count less.
public static class SectionReader
{
    private const double MustFactor = 1.5;
    private const double NiceFactor = 0.5;
    private const int MaxHeadingLength = 70;

    private static readonly string[] NiceMarkers =
    {
        "nice to have", "nice-to-have", "wünschenswert", "von vorteil", "idealerweise", "bonus", "ein plus", "a plus", "gerne auch"
    };

    private static readonly string[] MustMarkers =
    {
        "anforderungen", "ihr profil", "dein profil", "du bringst", "sie bringen", "qualifikation",
        "voraussetzungen", "requirements", "you bring", "what you bring", "must have", "your profile"
    };

    private static readonly string[] NeutralMarkers =
    {
        "aufgaben", "responsibilities", "wir bieten", "was wir bieten", "we offer", "benefits", "über uns", "about us"
    };

    public static IReadOnlyList<JobLine> Read(string jobDescription)
    {
        var result = new List<JobLine>();
        var sectionFactor = 1.0;

        foreach (var rawLine in Regex.Split(jobDescription, @"[\r\n]+|(?<=[.!?;])\s+"))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var lower = line.ToLowerInvariant();
            var isNiceLine = NiceMarkers.Any(lower.Contains);

            if (line.Length <= MaxHeadingLength)
            {
                if (isNiceLine) sectionFactor = NiceFactor;
                else if (MustMarkers.Any(lower.Contains)) sectionFactor = MustFactor;
                else if (NeutralMarkers.Any(lower.Contains)) sectionFactor = 1.0;
            }

            var factor = isNiceLine ? NiceFactor : sectionFactor;
            result.Add(new JobLine(TextNormalizer.ContentTokens(line).ToHashSet(), factor));
        }

        return result;
    }
}