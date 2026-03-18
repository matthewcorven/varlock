using Varlock.DotNet; // 👈 Varlock
using System.Text.Json;

namespace dotnet_winforms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        var runtime = new VarlockCliRuntime();
        var graph = runtime.Load(new VarlockLoadOptions());

        if (Environment.GetCommandLineArgs().Contains("--dump-config"))
        {
            var payload = new WinFormsConfigPayload(
                graph.Items.TryGetValue("APP_NAME", out var appName) ? appName.Value?.ToString() ?? string.Empty : string.Empty,
                graph.Items.TryGetValue("WINDOW_TITLE", out var windowTitle) ? windowTitle.Value?.ToString() ?? string.Empty : string.Empty,
                graph.Sources.Any((source) => source.Label.Contains(".env.schema", StringComparison.OrdinalIgnoreCase)));

            Console.WriteLine(JsonSerializer.Serialize(payload));
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1(graph));
    }
}

file sealed record WinFormsConfigPayload(string AppName, string WindowTitle, bool SchemaSourcePresent);
