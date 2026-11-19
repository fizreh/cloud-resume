namespace VisitorCounterApi.Analysis;

public sealed record MatchResult(
    int Score,
    string Grade,
    IReadOnlyList<string> Matched,
    IReadOnlyList<string> Missing,
    string KeywordSource);

public static class GradeScale
{
    public static string ToGrade(int score) => score switch
    {
        >= 85 => "A",
        >= 70 => "B",
        >= 55 => "C",
        >= 40 => "D",
        _ => "E"
    };
}