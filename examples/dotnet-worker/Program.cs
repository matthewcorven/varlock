using dotnet_worker;

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Varlock.Extensions.Hosting; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.AddVarlock(); // 👈 Varlock: load .env.schema into IConfiguration

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

if (args.Contains("--dump-config"))
{
	var configuration = host.Services.GetRequiredService<IConfiguration>();
	var payload = new WorkerConfigPayload(
		configuration["APP_NAME"] ?? string.Empty,
		configuration["WORKER_MESSAGE"] ?? string.Empty);

	Console.WriteLine(JsonSerializer.Serialize(payload));
	return;
}

host.Run();

sealed record WorkerConfigPayload(string AppName, string WorkerMessage);
