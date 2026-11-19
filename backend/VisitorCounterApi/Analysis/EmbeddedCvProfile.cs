namespace VisitorCounterApi.Analysis;

// Reads Analysis/cv.txt, which is embedded in the program (see the .csproj).
public sealed class EmbeddedCvProfile : ICvProfile
{
    private const string ResourceName = "cv.txt";

    public ISet<string> Tokens { get; }

    public EmbeddedCvProfile()
    {
        using var stream = typeof(EmbeddedCvProfile).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found. Check the .csproj.");
        using var reader = new StreamReader(stream);

        Tokens = TextNormalizer.ContentTokens(reader.ReadToEnd()).ToHashSet();
    }
}