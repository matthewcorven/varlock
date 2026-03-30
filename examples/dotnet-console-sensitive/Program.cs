using Varlock.DotNet; // 👈 Varlock

// 👈 Varlock: load the resolved graph directly for sensitivity-aware access
var runtime = new VarlockCliRuntime();
var graph = runtime.Load(new VarlockLoadOptions());

Console.WriteLine("Configuration values (sensitive values redacted):");
foreach (var item in graph.Items)
{
    // graph.Items still exposes the plaintext value in-process; redact only the display path.
    var rawValue = item.Value.Value?.ToString() ?? "(null)";
    var displayValue = VarlockRedactionHelper.Redact(graph, item.Key, rawValue); // 👈 Varlock: redact sensitive values
    Console.WriteLine($"  {item.Key} = {displayValue} (sensitive={item.Value.IsSensitive})");
}
