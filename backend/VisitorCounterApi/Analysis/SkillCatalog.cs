namespace VisitorCounterApi.Analysis;

// One known technology with its spelling variants ("Name|variant|variant").
public sealed class Skill
{
    private readonly IReadOnlyList<IReadOnlyList<string>> _variants;

    public string Name { get; }
    public IReadOnlyCollection<string> AllTokens { get; }

    public Skill(string definition)
    {
        var parts = definition.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Name = parts[0];
        _variants = parts
            .Select(part => TextNormalizer.ContentTokens(part))
            .Where(tokens => tokens.Count > 0)
            .ToList();
        AllTokens = _variants.SelectMany(tokens => tokens).ToHashSet();
    }

    public bool IsPresentIn(ISet<string> tokens) =>
        _variants.Any(variant => variant.All(tokens.Contains));
}

public static class SkillCatalog
{
    // IMPORTANT: Definitions must be declared BEFORE All (static fields are initialised top to bottom).
    private static readonly string[] Definitions =
    {
        "C#", ".NET", "ASP.NET", "Entity Framework|EF Core", "Java", "Spring", "Python", "TypeScript", "JavaScript",
        "Node.js", "NestJS", "Angular", "React", "Vue.js", "Next.js", "HTML", "CSS",
        "SQL", "PostgreSQL|Postgres", "MongoDB", "Oracle", "SQL Server|MSSQL|MS SQL", "MySQL", "Redis",
        "REST|RESTful|REST-API", "GraphQL", "API", "Microservices|Microservice",
        "Azure", "AWS|Amazon Web Services", "Google Cloud|GCP", "Cloud", "Serverless", "Azure Functions",
        "Docker", "Kubernetes|K8s", "Terraform", "Bicep", "Ansible", "Infrastructure as Code|IaC",
        "CI/CD", "Jenkins", "GitHub Actions", "Azure DevOps", "DevOps", "Git", "GitHub", "Bitbucket",
        "Jira", "Confluence", "Linux", "PowerShell", "Bash", "Monitoring", "Application Insights",
        "Scrum", "Agile", "Kanban", "SOLID", "Clean Code", "Unit Tests|Unit Testing|xUnit|NUnit|Jest", "Selenium",
        "Kafka", "RabbitMQ", "OpenAI", "Machine Learning"
    };

    public static IReadOnlyList<Skill> All { get; } = Definitions.Select(definition => new Skill(definition)).ToList();
}