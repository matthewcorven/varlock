using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration; // 👈 Varlock
using Varlock.Generated; // 👈 Varlock: build-time generated types from @generateTypes

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddVarlock(); // 👈 Varlock

using var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();

// 👈 Varlock: populate the generated VarlockConfig class using PropertyBindings metadata
var config = new VarlockConfig();
foreach (var binding in VarlockConfigMetadata.PropertyBindings)
{
    var raw = configuration[binding.Key];
    var prop = typeof(VarlockConfig).GetProperty(binding.PropertyName)!;
    if (raw is not null)
    {
        if (prop.PropertyType == typeof(string))
            prop.SetValue(config, raw);
        else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?))
            prop.SetValue(config, double.Parse(raw));
        else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
            prop.SetValue(config, bool.Parse(raw));
    }
}

Console.WriteLine("Typed configuration (from generated VarlockConfig):");
Console.WriteLine($"  config.AppName = {config.AppName}");
Console.WriteLine($"  config.HttpPort = {config.HttpPort}");
Console.WriteLine($"  config.DebugMode = {config.DebugMode}");

Console.WriteLine();
Console.WriteLine("Metadata (from generated VarlockConfigMetadata):");
foreach (var binding in VarlockConfigMetadata.PropertyBindings)
{
    Console.WriteLine($"  {binding.Key} -> {binding.PropertyName} (required={binding.IsRequired}, sensitive={binding.IsSensitive})");
}
