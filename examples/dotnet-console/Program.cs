using Varlock.DotNet; // 👈 Varlock

namespace dotnet_console;

class Program
{
  static void Main(string[] args)
  {
    Console.WriteLine("Hello, World!");

    // 👈 Varlock: load environment variables from .env.schema
    var runtime = new VarlockCliRuntime();
    var graph = runtime.Load(new VarlockLoadOptions());

    foreach (var item in graph.Items)
    {
      var display = item.Value.IsSensitive ? "***" : item.Value.Value?.ToString() ?? "(null)";
      Console.WriteLine($"  {item.Key} = {display}");
    }
  }
}
