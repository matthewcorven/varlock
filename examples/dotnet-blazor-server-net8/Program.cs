using System;
using System.Linq;
using System.Text.Json;
using DotnetBlazorServerNet8;
using DotnetBlazorServerNet8.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration;

var dumpConfig = args.Contains("--dump-config", StringComparer.Ordinal);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents();

// Varlock integrates directly into the IConfiguration chain.
// WebApplicationBuilder already loads appsettings.json and environment-specific overrides.
// By adding Varlock after those sources, Varlock keys override appsettings when the same key exists.
// This is honest coexistence: both remain active, and provider order determines precedence.
builder.Configuration.AddVarlock((source) =>
{
  source.WorkingDirectory = builder.Environment.ContentRootPath;
});

var app = builder.Build();

if (dumpConfig)
{
  var config = app.Services.GetRequiredService<IConfiguration>();
  Console.WriteLine(JsonSerializer.Serialize(BlazorConfigSnapshot.From(config)));
  return;
}

if (!builder.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();

app.Run();
