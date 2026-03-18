using System.Text.Json;
using dotnet_aspnet_mvc.Models;
using Varlock.Extensions.Hosting; // 👈 Varlock

var builder = WebApplication.CreateBuilder(args);

builder.AddVarlock(); // 👈 Varlock: wire Varlock into the WebApplicationBuilder configuration pipeline
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (args.Contains("--dump-config"))
{
  var payload = new MvcConfigPayload(
    app.Configuration["APP_NAME"] ?? string.Empty,
    app.Configuration["APPSETTINGS_ONLY"] ?? string.Empty,
    app.Configuration["USERSECRETS_ONLY"] ?? string.Empty);

  Console.WriteLine(JsonSerializer.Serialize(payload));
  return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

sealed record MvcConfigPayload(string AppName, string AppSettingsOnly, string UserSecretsOnly);
