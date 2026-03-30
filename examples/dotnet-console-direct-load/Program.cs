using Varlock.DotNet; // 👈 Varlock

// 👈 Varlock: load resolved values directly through the Varlock runtime — no IConfiguration, no hosting
var runtime = new VarlockCliRuntime();
var graph = runtime.Load(new VarlockLoadOptions());

Console.WriteLine("Resolved configuration:");
foreach (var item in graph.Items)
{
    // graph.Items keeps the raw plaintext value in-process; this sample masks what it writes to stdout.
    var display = item.Value.IsSensitive ? "***" : item.Value.Value?.ToString() ?? "(null)";
    Console.WriteLine($"  {item.Key} = {display}");
}

Console.WriteLine();
Console.WriteLine("Source files:");
foreach (var source in graph.Sources)
{
    Console.WriteLine($"  [{(source.Enabled ? "active" : "inactive")}] {source.Label}: {source.Path}");
}

Console.WriteLine();
Console.WriteLine($"RedactLogs = {graph.RedactLogs}");
Console.WriteLine($"PreventLeaks = {graph.PreventLeaks}");
