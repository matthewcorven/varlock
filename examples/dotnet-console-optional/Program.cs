using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);
var missingWorkingDirectory = Path.Combine(Environment.CurrentDirectory, "missing-config");

builder.Configuration.AddVarlock(source =>
{
    source.Optional = true; // 👈 Varlock: let the app start even if the configured schema entry point is missing
    source.WorkingDirectory = missingWorkingDirectory;
});

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var configurationRoot = (IConfigurationRoot)configuration;
var varlockProvider = configurationRoot.Providers.OfType<VarlockConfigurationProvider>().Single();

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"] ?? "(missing)"}");
Console.WriteLine($"HTTP_PORT = {configuration["HTTP_PORT"] ?? "(missing)"}");
Console.WriteLine($"VARLOCK_OPTIONAL = {varlockProvider.Source.Optional}");
Console.WriteLine($"VARLOCK_WORKING_DIRECTORY_NAME = {Path.GetFileName(varlockProvider.Source.WorkingDirectory)}");