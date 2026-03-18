using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration; // 👈 Varlock

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddVarlock(); // 👈 Varlock: compose final values from schema refs before flattening into IConfiguration

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();

Console.WriteLine($"API_BASE_URL = {configuration["API_BASE_URL"]}");
Console.WriteLine($"USERS_ENDPOINT = {configuration["USERS_ENDPOINT"]}");
Console.WriteLine($"ADMIN_ENDPOINT = {configuration["ADMIN_ENDPOINT"]}");