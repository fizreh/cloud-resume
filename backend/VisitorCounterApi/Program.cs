using DotNetEnv;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using VisitorCounterApi.Configuration;
using VisitorCounterApi.Data;
using VisitorCounterApi.Services;

// Local development only: load secrets from the .env file (searched upward from the working folder).
// NoClobber means real environment variables win over .env values.
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

// Dependency inversion: everything is wired together in this one place.
builder.Services.AddSingleton(sp => DatabaseSettings.FromConfiguration(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<IVisitorCounterRepository, SqlVisitorCounterRepository>();
builder.Services.AddScoped<IVisitorCounterService, VisitorCounterService>();

builder.Build().Run();