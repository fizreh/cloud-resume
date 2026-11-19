using DotNetEnv;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VisitorCounterApi.Analysis;
using VisitorCounterApi.Configuration;
using VisitorCounterApi.Data;
using VisitorCounterApi.Services;

// Local development only: load secrets from the .env file.
// In Azure there is no .env file, so a missing file is expected and ignored.
try
{
    Env.NoClobber().TraversePath().Load();
}
catch (FileNotFoundException)
{
}

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Visitor counter
builder.Services.AddSingleton(sp => DatabaseSettings.FromConfiguration(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<IVisitorCounterRepository, SqlVisitorCounterRepository>();
builder.Services.AddScoped<IVisitorCounterService, VisitorCounterService>();

// Job-match analyser
builder.Services.AddSingleton(sp => LanguageSettings.FromConfiguration(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<ICvProfile, EmbeddedCvProfile>();
builder.Services.AddSingleton(new FixedWindowRateLimiter(maxRequests: 30, window: TimeSpan.FromMinutes(10)));
builder.Services.AddHttpClient<AzureLanguageKeywordExtractor>(client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<LocalKeywordExtractor>();
builder.Services.AddScoped<IKeywordExtractor>(sp => new FallbackKeywordExtractor(
    sp.GetRequiredService<AzureLanguageKeywordExtractor>(),
    sp.GetRequiredService<LocalKeywordExtractor>(),
    sp.GetRequiredService<ILogger<FallbackKeywordExtractor>>()));
builder.Services.AddScoped<IJobMatchAnalyzer, JobMatchAnalyzer>();

builder.Build().Run();