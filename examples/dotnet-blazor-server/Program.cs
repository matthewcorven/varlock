using dotnet_blazor_server.Components;
using System.Text.Json;
using Varlock.Extensions.Hosting; // 👈 Varlock

var builder = WebApplication.CreateBuilder(args);

builder.AddVarlock(); // 👈 Varlock: wire Varlock into the Blazor Server host builder

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (args.Contains("--dump-config"))
{
    var payload = new BlazorServerPayload(
        app.Configuration["APP_NAME"] ?? string.Empty,
        app.Configuration["COMPONENT_MESSAGE"] ?? string.Empty);

    Console.WriteLine(JsonSerializer.Serialize(payload));
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

sealed record BlazorServerPayload(string AppName, string ComponentMessage);
