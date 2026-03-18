using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.DotNet;
using Varlock.Extensions.Configuration; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);
var runtime = new EnvironmentAwareRuntime();

builder.Configuration.AddVarlock(source =>
{
    source.EnvironmentName = "production"; // 👈 Varlock: forward the requested environment name into the runtime load options
    source.Runtime = runtime;
});

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"]}");
Console.WriteLine($"API_BASE_URL = {configuration["API_BASE_URL"]}");
Console.WriteLine("VARLOCK_ENVIRONMENT_NAME = production");

file sealed class EnvironmentAwareRuntime : IVarlockRuntime
{
    public VarlockResolvedGraph Load(VarlockLoadOptions options)
    {
        var isProduction = string.Equals(options.EnvironmentName, "production", StringComparison.OrdinalIgnoreCase);

        var items = new Dictionary<string, VarlockResolvedItem>
        {
            ["APP_NAME"] = new("APP_NAME", isProduction ? "varlock-production" : "varlock-development", false),
            ["API_BASE_URL"] = new("API_BASE_URL", isProduction ? "https://api.production.varlock.test" : "https://api.development.varlock.test", false),
        };

        var sources = new List<VarlockSourceInfo>
        {
            new("environment-name", true, options.SchemaPath),
        };

        return new VarlockResolvedGraph(items, sources, redactLogs: false, preventLeaks: false, options.GetWorkingDirectory(), contractVersion: 1);
    }

    public Task<VarlockResolvedGraph> LoadAsync(VarlockLoadOptions options, CancellationToken cancellationToken = default) =>
        Task.FromResult(Load(options));
}