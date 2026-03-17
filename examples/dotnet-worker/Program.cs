using dotnet_worker;

using Varlock.Extensions.Hosting; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.AddVarlock(); // 👈 Varlock: load .env.schema into IConfiguration

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
