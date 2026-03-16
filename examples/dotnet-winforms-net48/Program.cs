using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Varlock.DotNet;

namespace VarlockWinFormsExample
{
  static class Program
  {
    [STAThread]
    static void Main(string[] args)
    {
      // Legacy net48 desktop bridge proof: load Varlock configuration,
      // validate values, serialize to JSON
      var runtime = new VarlockCliRuntime();
      var loadOptions = new VarlockLoadOptions
      {
        WorkingDirectory = Directory.GetCurrentDirectory(),
      };

      var graph = runtime.Load(loadOptions);

      var payload = new
      {
        appName = GetString(graph, "APP_NAME"),
        httpPort = GetInt32(graph, "HTTP_PORT"),
        featureEnabled = GetBoolean(graph, "FEATURE_ENABLED"),
        secretIsSensitive = GetItem(graph, "SECRET_TOKEN").IsSensitive,
        redactLogs = graph.RedactLogs,
        preventLeaks = graph.PreventLeaks,
        sourceLabels = graph.Sources.Select((source) => source.Label).ToArray(),
      };

      var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

      // Proof mode: emit JSON to stdout and exit without UI
      if (args.Length > 0 && string.Equals(args[0], "--dump-config", StringComparison.OrdinalIgnoreCase))
      {
        Console.WriteLine(json);
        return;
      }

      // Interactive mode: display configuration in MessageBox
      MessageBox.Show(
        json,
        "Varlock Configuration Loaded",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);
    }

    static VarlockResolvedItem GetItem(VarlockResolvedGraph graph, string key)
    {
      if (!graph.Items.TryGetValue(key, out var item))
      {
        throw new InvalidOperationException($"Expected Varlock item '{key}' to be present.");
      }

      return item;
    }

    static string GetString(VarlockResolvedGraph graph, string key)
    {
      var value = GetItem(graph, key).Value;
      return value as string
        ?? throw new InvalidOperationException($"Expected Varlock item '{key}' to resolve to a string.");
    }

    static int GetInt32(VarlockResolvedGraph graph, string key)
    {
      var value = GetItem(graph, key).Value;
      return value switch
      {
        int intValue => intValue,
        long longValue => checked((int)longValue),
        decimal decimalValue => decimal.ToInt32(decimalValue),
        double doubleValue => checked((int)doubleValue),
        string stringValue => int.Parse(stringValue, CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException($"Expected Varlock item '{key}' to resolve to an integer-compatible value."),
      };
    }

    static bool GetBoolean(VarlockResolvedGraph graph, string key)
    {
      var value = GetItem(graph, key).Value;
      return value switch
      {
        bool boolValue => boolValue,
        string stringValue => bool.Parse(stringValue),
        _ => throw new InvalidOperationException($"Expected Varlock item '{key}' to resolve to a boolean-compatible value."),
      };
    }
  }
}
