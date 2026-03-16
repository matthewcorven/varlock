using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Varlock.DotNet;

var runtime = new VarlockCliRuntime();
var loadOptions = new VarlockLoadOptions
{
  WorkingDirectory = Directory.GetCurrentDirectory(),
};
var redactionHelperProof = args.Contains("--redaction-helper-proof", StringComparer.Ordinal);

if (string.Equals(
  Environment.GetEnvironmentVariable("VARLOCK_DOTNET_PROOF_FORCE_PATH_LOOKUP"),
  "1",
  StringComparison.Ordinal))
{
  loadOptions.EnableLocalExecutableLookup = false;
  loadOptions.EnablePathLookup = true;
}

var graph = runtime.Load(loadOptions);

if (redactionHelperProof)
{
  var secretItem = GetItem(graph, "SECRET_TOKEN");
  var secretValue = GetString(graph, "SECRET_TOKEN");
  var appName = GetString(graph, "APP_NAME");

  Console.WriteLine(JsonSerializer.Serialize(new
  {
    secretIsSensitive = secretItem.IsSensitive,
    rawSecret = secretValue,
    helperRedactedSecret = VarlockRedactionHelper.Redact(graph, "SECRET_TOKEN", secretValue),
    helperCaseMismatchSecret = VarlockRedactionHelper.Redact(graph, "secret_token", secretValue),
    rawAppName = appName,
    helperAppName = VarlockRedactionHelper.Redact(graph, "APP_NAME", appName),
    redactLogs = graph.RedactLogs,
    preventLeaks = graph.PreventLeaks,
  }));
  return;
}

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

Console.WriteLine(JsonSerializer.Serialize(payload));

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
