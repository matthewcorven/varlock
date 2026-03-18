using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration; // 👈 Varlock

const string ExplicitExecutablePath = "../../packages/varlock/bin/cli.js";

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddVarlock(source =>
{
    source.ExecutablePath = ExplicitExecutablePath; // 👈 Varlock: prove the explicit override path instead of automatic discovery
    source.EnableLocalExecutableLookup = false;
    source.EnablePathLookup = false;
});

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var configurationRoot = (IConfigurationRoot)configuration;
var provider = configurationRoot.Providers.OfType<VarlockConfigurationProvider>().Single();

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"]}");
Console.WriteLine($"EXECUTABLE_PATH = {provider.Source.ExecutablePath}");
Console.WriteLine($"LOCAL_LOOKUP = {provider.Source.EnableLocalExecutableLookup}");
Console.WriteLine($"PATH_LOOKUP = {provider.Source.EnablePathLookup}");