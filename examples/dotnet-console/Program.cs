using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddVarlock(); // 👈 Varlock: load the default .env.schema and .env files into IConfiguration

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>(); // 👈 Varlock: read values through the standard configuration path
var configurationRoot = (IConfigurationRoot)configuration;
var varlockProvider = configurationRoot.Providers.OfType<VarlockConfigurationProvider>().Single();

Console.WriteLine($"APP_NAME = {configuration["APP_NAME"]}");
Console.WriteLine($"HTTP_PORT = {configuration["HTTP_PORT"]}");
Console.WriteLine($"FEATURE_ENABLED = {configuration["FEATURE_ENABLED"]}");
Console.WriteLine($"VARLOCK_PROVIDER = {varlockProvider.GetType().Name}");
Console.WriteLine($"VARLOCK_SCHEMA_PATH = {varlockProvider.Source.SchemaPath}");
