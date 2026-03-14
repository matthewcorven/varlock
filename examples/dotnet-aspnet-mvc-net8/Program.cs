using System;
using System.Linq;
using System.Text.Json;
using DotnetAspNetMvcNet8;
using Varlock.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddVarlock((source) =>
{
  source.WorkingDirectory = builder.Environment.ContentRootPath;
});

builder.Services.AddControllersWithViews();

var snapshot = AppConfigSnapshot.From(builder.Configuration);
if (args.Contains("--dump-config", StringComparer.Ordinal))
{
  Console.WriteLine(JsonSerializer.Serialize(snapshot));
  return;
}

var app = builder.Build();
app.MapControllers();
app.Run();