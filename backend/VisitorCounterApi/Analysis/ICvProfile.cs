namespace VisitorCounterApi.Analysis;

public interface ICvProfile
{
    // All meaningful tokens of the CV.
    ISet<string> Tokens { get; }
}