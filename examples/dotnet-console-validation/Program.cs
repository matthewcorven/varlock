using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.DotNet;
using Varlock.Extensions.Configuration; // 👈 Varlock

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Configuration.AddVarlock(); // 👈 Varlock: startup load should fail because REQUIRED_TOKEN resolves empty

    using var app = builder.Build();
    var configuration = app.Services.GetRequiredService<IConfiguration>();

    Console.WriteLine($"UNEXPECTED_SUCCESS = {configuration["APP_NAME"]}");
}
catch (VarlockBridgeException ex)
{
    Console.WriteLine($"VALIDATION_CATEGORY = {ex.Category}");
    Console.WriteLine($"VALIDATION_MESSAGE = {ex.Message.Replace(Environment.NewLine, " ")}");
    Environment.ExitCode = 1;
}