using Serilog;
using Varlock.DotNet; // 👈 Varlock
using Varlock.Serilog; // 👈 Varlock: Serilog integration

// 👈 Varlock: load the resolved graph directly
var runtime = new VarlockCliRuntime();
var graph = runtime.Load(new VarlockLoadOptions());

// 👈 Varlock: configure Serilog with redaction and metadata enrichment
using var logger = new LoggerConfiguration()
    .Destructure.WithVarlockRedaction(graph) // 👈 Varlock: redact @sensitive values in destructured objects
    .Enrich.WithVarlockMetadata(graph) // 👈 Varlock: add VarlockRedactLogs property to events
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Properties:j}{NewLine}")
    .CreateLogger();

// Log a config object — sensitive values are automatically redacted
var config = new
{
    APP_NAME = graph.Items["APP_NAME"].Value,
    API_KEY = graph.Items["API_KEY"].Value, // @sensitive — will show as [REDACTED]
    FEATURE_FLAG = graph.Items["FEATURE_FLAG"].Value,
};

Console.WriteLine("Logging configuration object (sensitive values are redacted):");
logger.Information("Application config: {@Config}", config);

Console.WriteLine();
Console.WriteLine("Direct values for verification:");
foreach (var item in graph.Items)
{
    Console.WriteLine($"  {item.Key} = {(item.Value.IsSensitive ? "[REDACTED]" : item.Value.Value?.ToString() ?? "(null)")} (sensitive={item.Value.IsSensitive})");
}
