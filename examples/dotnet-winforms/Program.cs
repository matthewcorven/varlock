using Varlock.DotNet; // 👈 Varlock

namespace dotnet_winforms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 👈 Varlock: load environment variables from .env.schema
        var runtime = new VarlockCliRuntime();
        var graph = runtime.Load(new VarlockLoadOptions());

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1(graph)); // 👈 Varlock: pass graph to display config values
    }
}
