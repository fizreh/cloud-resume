namespace VisitorCounterApi.Analysis;

// Score = share of the weighted requirements of the job text that are found in the CV.
public sealed class JobMatchAnalyzer : IJobMatchAnalyzer
{
    private const double SkillWeight = 3.0;            // known technologies count more
    private const double PhraseWeight = 1.0;           // other key phrases count less
    private const double PhraseMatchThreshold = 0.66;  // share of a phrase's words that must be in the CV
    private const int MaxPhrases = 25;
    private const int MaxListed = 15;

    private sealed record Requirement(string Label, double Weight, bool Matched);

    private readonly IKeywordExtractor _extractor;
    private readonly ICvProfile _cv;

    public JobMatchAnalyzer(IKeywordExtractor extractor, ICvProfile cv)
    {
        _extractor = extractor;
        _cv = cv;
    }

    public async Task<MatchResult> AnalyzeAsync(string jobDescription, CancellationToken cancellationToken = default)
    {
        var lines = SectionReader.Read(jobDescription);
        var requirements = new List<Requirement>();

        // 1) Known technologies that appear in the job text
        var skillTokens = new HashSet<string>();
        foreach (var skill in SkillCatalog.All)
        {
            var factors = lines.Where(line => skill.IsPresentIn(line.Tokens)).Select(line => line.Factor).ToList();
            if (factors.Count == 0)
            {
                continue;
            }

            requirements.Add(new Requirement(skill.Name, SkillWeight * factors.Max(), skill.IsPresentIn(_cv.Tokens)));
            skillTokens.UnionWith(skill.AllTokens);
        }

        // 2) Other key phrases from the extractor (Azure AI Language or local fallback)
        var extraction = await _extractor.ExtractAsync(jobDescription, cancellationToken);
        var seen = new HashSet<string>();
        var phraseCount = 0;

        foreach (var phrase in extraction.Phrases)
        {
            if (phraseCount >= MaxPhrases)
            {
                break;
            }

            var tokens = TextNormalizer.ContentTokens(phrase);
            if (tokens.Count == 0 || tokens.Count > 4 || tokens.All(skillTokens.Contains))
            {
                continue;
            }

            if (!seen.Add(string.Join(' ', tokens.OrderBy(token => token))))
            {
                continue;
            }

            var factor = lines
                .Where(line => tokens.All(line.Tokens.Contains))
                .Select(line => line.Factor)
                .DefaultIfEmpty(1.0)
                .Max();

            var coverage = tokens.Count(_cv.Tokens.Contains) / (double)tokens.Count;
            requirements.Add(new Requirement(phrase.Trim(), PhraseWeight * factor, coverage >= PhraseMatchThreshold));
            phraseCount++;
        }

        if (requirements.Count == 0)
        {
            return new MatchResult(0, GradeScale.ToGrade(0), [], [], extraction.Source);
        }

        var total = requirements.Sum(requirement => requirement.Weight);
        var achieved = requirements.Where(requirement => requirement.Matched).Sum(requirement => requirement.Weight);
        var score = (int)Math.Round(100 * achieved / total, MidpointRounding.AwayFromZero);

        return new MatchResult(
            score,
            GradeScale.ToGrade(score),
            Listed(requirements.Where(requirement => requirement.Matched)),
            Listed(requirements.Where(requirement => !requirement.Matched)),
            extraction.Source);
    }

    private static List<string> Listed(IEnumerable<Requirement> requirements) =>
        requirements
            .OrderByDescending(requirement => requirement.Weight)
            .Select(requirement => requirement.Label)
            .Take(MaxListed)
            .ToList();
}