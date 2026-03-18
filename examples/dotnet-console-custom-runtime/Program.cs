using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.DotNet;
using Varlock.Extensions.Configuration; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);
var runtime = new FakeVarlockRuntime();

builder.Configuration.AddVarlock(source =>
{
    source.Runtime = runtime; // 👈 Varlock: replace the CLI bridge with an injected runtime for tests or custom hosts
});

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"]}");
Console.WriteLine($"HTTP_PORT = {configuration["HTTP_PORT"]}");
Console.WriteLine($"FEATURE_ENABLED = {configuration["FEATURE_ENABLED"]}");
Console.WriteLine($"RUNTIME_TYPE = {nameof(FakeVarlockRuntime)}");

file sealed class FakeVarlockRuntime : IVarlockRuntime
{
    public VarlockResolvedGraph Load(VarlockLoadOptions options)
    {
        var items = new Dictionary<string, VarlockResolvedItem>
        {
            ["APP_NAME"] = new("APP_NAME", "varlock-custom-runtime", false),
            ["HTTP_PORT"] = new("HTTP_PORT", 4343, false),
            ["FEATURE_ENABLED"] = new("FEATURE_ENABLED", true, false),
        };

        var sources = new List<VarlockSourceInfo>
        {
            new("custom-runtime", true, options.SchemaPath),
        };

        return new VarlockResolvedGraph(items, sources, redactLogs: false, preventLeaks: false, options.GetWorkingDirectory(), contractVersion: 1);
    }

    public Task<VarlockResolvedGraph> LoadAsync(VarlockLoadOptions options, CancellationToken cancellationToken = default) =>
        Task.FromResult(Load(options));
}