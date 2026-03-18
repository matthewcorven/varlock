using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);
var workingDirectory = Path.Combine(Environment.CurrentDirectory, "shared");

builder.Configuration.AddVarlock(source =>
{
    source.WorkingDirectory = workingDirectory; // 👈 Varlock: resolve the default .env.schema from a different directory
});

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var configurationRoot = (IConfigurationRoot)configuration;
var varlockProvider = configurationRoot.Providers.OfType<VarlockConfigurationProvider>().Single();

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"]}");
Console.WriteLine($"HTTP_PORT = {configuration["HTTP_PORT"]}");
Console.WriteLine($"VARLOCK_SCHEMA_PATH = {varlockProvider.Source.SchemaPath}");
Console.WriteLine($"VARLOCK_WORKING_DIRECTORY_NAME = {Path.GetFileName(varlockProvider.Source.WorkingDirectory)}");