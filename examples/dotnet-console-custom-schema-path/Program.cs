using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddVarlock(source =>
{
    source.SchemaPath = "config/.env.schema"; // 👈 Varlock: load schema and values from a non-default path
});

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var configurationRoot = (IConfigurationRoot)configuration;
var varlockProvider = configurationRoot.Providers.OfType<VarlockConfigurationProvider>().Single();

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"]}");
Console.WriteLine($"HTTP_PORT = {configuration["HTTP_PORT"]}");
Console.WriteLine($"VARLOCK_SCHEMA_PATH = {varlockProvider.Source.SchemaPath}");