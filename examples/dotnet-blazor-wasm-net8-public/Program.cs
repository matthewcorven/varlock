using System;
using System.Threading.Tasks;
using DotnetBlazorWasmNet8Public;
using DotnetBlazorWasmNet8Public.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register the generated public-only config as a singleton.
// This is a POCO — no runtime Varlock bridge, no CLI invocation, no IConfiguration provider.
// The generated .g.cs is the ONLY Varlock artifact in the WASM bundle.
builder.Services.AddSingleton(new DotnetBlazorWasmNet8Public.Generated.VarlockPublicConfig
{
  AppName = "varlock-blazor-wasm",
  AppPort = 5281,
  FeatureEnabled = true
});

builder.Services.AddScoped(sp => new System.Net.Http.HttpClient
{
  BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

await builder.Build().RunAsync();
