using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

var exampleRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../"));
var envLocalPath = Path.Combine(exampleRoot, ".env.local");

// Seed the vault URL into process env so the root decorator sees the live value during bridge startup.
if (File.Exists(envLocalPath))
{
	var vaultUrlLine = File.ReadLines(envLocalPath)
		.Select(line => line.Trim())
		.FirstOrDefault(line => line.StartsWith("AZURE_KEY_VAULT_URL=", StringComparison.Ordinal));

	if (vaultUrlLine is not null)
	{
		var vaultUrl = vaultUrlLine["AZURE_KEY_VAULT_URL=".Length..].Trim();
		if (!string.IsNullOrWhiteSpace(vaultUrl))
		{
			Environment.SetEnvironmentVariable("AZURE_KEY_VAULT_URL", vaultUrl);
		}
	}
}

builder.Configuration.AddVarlock(source =>
{
	source.WorkingDirectory = exampleRoot;
});

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var configurationRoot = (IConfigurationRoot)configuration;
var varlockProvider = configurationRoot.Providers.OfType<VarlockConfigurationProvider>().Single();

var databaseUrl = configuration["DATABASE_URL"];
var stripeSecretKey = configuration["STRIPE_SECRET_KEY"];

Console.WriteLine($"AZURE_KEY_VAULT_URL = {configuration["AZURE_KEY_VAULT_URL"]}");
Console.WriteLine($"PUBLIC_BASE_URL = {configuration["PUBLIC_BASE_URL"]}");
Console.WriteLine($"DATABASE_URL loaded = {!string.IsNullOrWhiteSpace(databaseUrl)}");
Console.WriteLine($"STRIPE_SECRET_KEY loaded = {!string.IsNullOrWhiteSpace(stripeSecretKey)}");
Console.WriteLine($"VARLOCK_PROVIDER = {varlockProvider.GetType().Name}");