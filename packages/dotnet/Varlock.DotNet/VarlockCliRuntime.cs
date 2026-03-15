using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Varlock.DotNet;

public sealed class VarlockCliRuntime : IVarlockRuntime
{
  private const int SupportedContractVersion = 1;
  private const int HandshakeProbeContractVersion = 0;
  private const string ExecutableName = "varlock";
  private static readonly string[] WindowsExecutableExtensions = { ".exe", ".cmd", ".bat" };

  public VarlockResolvedGraph Load(VarlockLoadOptions options)
  {
    if (options is null)
    {
      throw new ArgumentNullException(nameof(options));
    }

    var schemaFullPath = options.GetSchemaFullPath();
    if (!File.Exists(schemaFullPath))
    {
      throw new VarlockBridgeException(
        VarlockBridgeErrorCategory.SchemaMissing,
        $"Unable to locate the Varlock schema at '{schemaFullPath}'.",
        filePath: schemaFullPath);
    }

    var executablePath = ResolveExecutable(options);
    var workingDirectory = options.GetWorkingDirectory();
    var handshakeResult = RunProcess(
      executablePath,
      BuildHandshakeArguments(),
      workingDirectory,
      options.EnvironmentVariables,
      startupFailureCategory: VarlockBridgeErrorCategory.ExecutableNotFound,
      startupFailureMessage: $"Unable to start the Varlock executable at '{executablePath}' for the bridge handshake.");

    EnsureExecutableSupportsBridgeContract(
      handshakeResult.StandardOutput,
      handshakeResult.StandardError,
      handshakeResult.ExitCode,
      executablePath);

    var loadResult = RunProcess(
      executablePath,
      BuildLoadArguments(options),
      workingDirectory,
      options.EnvironmentVariables,
      startupFailureCategory: VarlockBridgeErrorCategory.BridgeInternalError,
      startupFailureMessage: $"The Varlock executable at '{executablePath}' passed the bridge handshake but failed to start for the load command.");

    return ParseCliOutput(loadResult.StandardOutput, loadResult.StandardError, loadResult.ExitCode);
  }

  public Task<VarlockResolvedGraph> LoadAsync(VarlockLoadOptions options, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(Load(options));
  }

  internal static VarlockResolvedGraph ParseCliOutput(string standardOutput, string? standardError, int exitCode)
  {
    if (string.IsNullOrWhiteSpace(standardOutput))
    {
      throw CreateMissingPayloadException(exitCode, standardOutput, standardError);
    }

    try
    {
      using var document = JsonDocument.Parse(standardOutput);
      var root = document.RootElement;

      if (LooksLikeBridgeEnvelope(root))
      {
        return ParseBridgeEnvelope(root, standardOutput, standardError, exitCode);
      }

      if (exitCode != 0)
      {
        throw CreateMissingPayloadException(exitCode, standardOutput, standardError);
      }

      return ParseResolvedGraph(root, contractVersion: null);
    }
    catch (JsonException ex)
    {
      throw new VarlockBridgeException(
        VarlockBridgeErrorCategory.BridgeInternalError,
        "The Varlock CLI returned an invalid machine-readable payload.",
        ex,
        exitCode: exitCode,
        standardOutput: standardOutput,
        standardError: standardError);
    }
  }

  private static void ApplyEnvironment(ProcessStartInfo startInfo, IReadOnlyDictionary<string, string?>? environmentVariables)
  {
    if (environmentVariables is null)
    {
      return;
    }

    foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
    {
      var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
      var value = Convert.ToString(entry.Value, CultureInfo.InvariantCulture);
      if (key is not null && value is not null && !startInfo.EnvironmentVariables.ContainsKey(key))
      {
        startInfo.EnvironmentVariables[key] = value;
      }
    }

    foreach (var pair in environmentVariables)
    {
      if (pair.Value is null)
      {
        startInfo.EnvironmentVariables.Remove(pair.Key);
      }
      else
      {
        startInfo.EnvironmentVariables[pair.Key] = pair.Value;
      }
    }
  }

  private static string BuildLoadArguments(VarlockLoadOptions options)
  {
    var arguments = new List<string>
    {
      "load",
      "--format",
      "json-full",
      "--bridge-contract",
      SupportedContractVersion.ToString(CultureInfo.InvariantCulture),
      "--compact",
      "--path",
      options.GetSchemaFullPath(),
    };

    if (!string.IsNullOrWhiteSpace(options.EnvironmentName))
    {
      arguments.Add("--env");
      arguments.Add(options.EnvironmentName!);
    }

    return string.Join(" ", arguments.Select(QuoteArgument));
  }

  private static string BuildHandshakeArguments()
  {
    var arguments = new[]
    {
      "load",
      "--bridge-contract",
      HandshakeProbeContractVersion.ToString(CultureInfo.InvariantCulture),
      "--compact",
    };

    return string.Join(" ", arguments.Select(QuoteArgument));
  }

  private static bool LooksLikeBridgeEnvelope(JsonElement root)
  {
    return root.ValueKind == JsonValueKind.Object
      && (root.TryGetProperty("ok", out _)
        || root.TryGetProperty("graph", out _)
        || root.TryGetProperty("category", out _)
        || root.TryGetProperty("command", out _));
  }

  private static VarlockResolvedGraph ParseBridgeEnvelope(
    JsonElement root,
    string standardOutput,
    string? standardError,
    int exitCode)
  {
    var contractVersion = GetRequiredInt32Property(root, "contractVersion", standardOutput, standardError, exitCode);
    if (contractVersion != SupportedContractVersion)
    {
      throw new VarlockBridgeException(
        VarlockBridgeErrorCategory.ExecutableVersionMismatch,
        $"Unsupported Varlock bridge contract version '{contractVersion}'. Expected '{SupportedContractVersion}'.",
        exitCode: exitCode,
        standardOutput: standardOutput,
        standardError: standardError);
    }

    var ok = GetRequiredBooleanProperty(root, "ok", standardOutput, standardError, exitCode);
    if (ok)
    {
      if (exitCode != 0)
      {
        throw new VarlockBridgeException(
          VarlockBridgeErrorCategory.BridgeInternalError,
          $"The Varlock CLI reported a successful bridge payload but exited with code {exitCode}.",
          exitCode: exitCode,
          standardOutput: standardOutput,
          standardError: standardError);
      }

      if (!root.TryGetProperty("graph", out var graphElement) || graphElement.ValueKind != JsonValueKind.Object)
      {
        throw new VarlockBridgeException(
          VarlockBridgeErrorCategory.BridgeInternalError,
          "The Varlock CLI bridge payload did not include a graph object.",
          exitCode: exitCode,
          standardOutput: standardOutput,
          standardError: standardError);
      }

      return ParseResolvedGraph(graphElement, contractVersion);
    }

    throw CreateBridgeFailureException(root, standardOutput, standardError, exitCode);
  }

  internal static void EnsureExecutableSupportsBridgeContract(
    string standardOutput,
    string? standardError,
    int exitCode,
    string executablePath)
  {
    if (string.IsNullOrWhiteSpace(standardOutput))
    {
      throw CreateHandshakeFailureException(
        executablePath,
        "The Varlock executable did not return a bridge handshake payload.",
        exitCode,
        standardOutput,
        standardError);
    }

    try
    {
      using var document = JsonDocument.Parse(standardOutput);
      var root = document.RootElement;
      if (!LooksLikeBridgeEnvelope(root))
      {
        throw CreateHandshakeFailureException(
          executablePath,
          "The Varlock executable did not return a recognized bridge handshake payload.",
          exitCode,
          standardOutput,
          standardError);
      }

      var category = root.TryGetProperty("category", out var categoryElement) && categoryElement.ValueKind == JsonValueKind.String
        ? categoryElement.GetString()
        : null;
      if (!string.Equals(category, "executable-version-mismatch", StringComparison.Ordinal))
      {
        throw CreateHandshakeFailureException(
          executablePath,
          "The Varlock executable did not report bridge compatibility during the handshake probe.",
          exitCode,
          standardOutput,
          standardError);
      }

      var supportedContractVersion = root.TryGetProperty("supportedContractVersion", out var supportedContractVersionElement)
        && supportedContractVersionElement.ValueKind == JsonValueKind.Number
          ? supportedContractVersionElement.GetInt32()
          : (int?)null;
      if (supportedContractVersion != SupportedContractVersion)
      {
        throw CreateHandshakeFailureException(
          executablePath,
          supportedContractVersion is null
            ? $"The Varlock executable did not declare support for bridge contract version {SupportedContractVersion}."
            : $"The Varlock executable reported bridge contract version {supportedContractVersion}, but version {SupportedContractVersion} is required.",
          exitCode,
          standardOutput,
          standardError);
      }
    }
    catch (JsonException ex)
    {
      throw new VarlockBridgeException(
        VarlockBridgeErrorCategory.ExecutableVersionMismatch,
        $"The Varlock executable at '{executablePath}' did not return valid JSON for the bridge handshake.",
        ex,
        exitCode: exitCode,
        standardOutput: standardOutput,
        standardError: standardError,
        filePath: executablePath);
    }
  }

  private static VarlockBridgeException CreateBridgeFailureException(
    JsonElement root,
    string standardOutput,
    string? standardError,
    int exitCode)
  {
    var category = root.TryGetProperty("category", out var categoryElement) && categoryElement.ValueKind == JsonValueKind.String
      ? ParseFailureCategory(categoryElement.GetString())
      : VarlockBridgeErrorCategory.BridgeInternalError;

    var message = root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String
      ? messageElement.GetString()
      : null;

    string? filePath = null;
    int? line = null;
    int? column = null;

    if (root.TryGetProperty("location", out var locationElement) && locationElement.ValueKind == JsonValueKind.Object)
    {
      if (locationElement.TryGetProperty("file", out var fileElement) && fileElement.ValueKind == JsonValueKind.String)
      {
        filePath = fileElement.GetString();
      }

      if (locationElement.TryGetProperty("line", out var lineElement) && lineElement.ValueKind == JsonValueKind.Number)
      {
        line = lineElement.GetInt32();
      }

      if (locationElement.TryGetProperty("column", out var columnElement) && columnElement.ValueKind == JsonValueKind.Number)
      {
        column = columnElement.GetInt32();
      }
    }

    return new VarlockBridgeException(
      category,
      message ?? "The Varlock CLI reported a bridge failure.",
      exitCode: exitCode,
      standardOutput: standardOutput,
      standardError: standardError,
      filePath: filePath,
      line: line,
      column: column);
  }

  private static int GetRequiredInt32Property(
    JsonElement root,
    string propertyName,
    string standardOutput,
    string? standardError,
    int exitCode)
  {
    if (root.TryGetProperty(propertyName, out var propertyElement) && propertyElement.ValueKind == JsonValueKind.Number)
    {
      return propertyElement.GetInt32();
    }

    throw new VarlockBridgeException(
      VarlockBridgeErrorCategory.BridgeInternalError,
      $"The Varlock CLI bridge payload did not include a valid '{propertyName}' property.",
      exitCode: exitCode,
      standardOutput: standardOutput,
      standardError: standardError);
  }

  private static bool GetRequiredBooleanProperty(
    JsonElement root,
    string propertyName,
    string standardOutput,
    string? standardError,
    int exitCode)
  {
    if (root.TryGetProperty(propertyName, out var propertyElement)
        && propertyElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
    {
      return propertyElement.GetBoolean();
    }

    throw new VarlockBridgeException(
      VarlockBridgeErrorCategory.BridgeInternalError,
      $"The Varlock CLI bridge payload did not include a valid '{propertyName}' property.",
      exitCode: exitCode,
      standardOutput: standardOutput,
      standardError: standardError);
  }

  private static VarlockBridgeException CreateMissingPayloadException(
    int exitCode,
    string? standardOutput,
    string? standardError)
  {
    var message = exitCode != 0
      ? $"The Varlock CLI exited with code {exitCode} without returning a valid bridge payload."
      : "The Varlock CLI returned an empty machine-readable payload.";

    return new VarlockBridgeException(
      VarlockBridgeErrorCategory.BridgeInternalError,
      message,
      exitCode: exitCode,
      standardOutput: standardOutput,
      standardError: standardError);
  }

  private static VarlockBridgeException CreateHandshakeFailureException(
    string executablePath,
    string message,
    int exitCode,
    string? standardOutput,
    string? standardError)
  {
    return new VarlockBridgeException(
      VarlockBridgeErrorCategory.ExecutableVersionMismatch,
      message,
      exitCode: exitCode,
      standardOutput: standardOutput,
      standardError: standardError,
      filePath: executablePath);
  }

  private static VarlockBridgeErrorCategory ParseFailureCategory(string? category)
  {
    return category switch
    {
      "executable-version-mismatch" => VarlockBridgeErrorCategory.ExecutableVersionMismatch,
      "schema-missing" => VarlockBridgeErrorCategory.SchemaMissing,
      "schema-invalid" => VarlockBridgeErrorCategory.SchemaInvalid,
      "resolution-failed" => VarlockBridgeErrorCategory.ResolutionFailed,
      "plugin-load-failed" => VarlockBridgeErrorCategory.PluginLoadFailed,
      "bridge-internal-error" => VarlockBridgeErrorCategory.BridgeInternalError,
      _ => VarlockBridgeErrorCategory.BridgeInternalError,
    };
  }

  private static VarlockResolvedGraph ParseResolvedGraph(JsonElement root, int? contractVersion)
  {
    var items = new Dictionary<string, VarlockResolvedItem>(StringComparer.Ordinal);
    if (root.TryGetProperty("config", out var configElement) && configElement.ValueKind == JsonValueKind.Object)
    {
      foreach (var property in configElement.EnumerateObject())
      {
        var item = property.Value;
        object? value = null;
        var isSensitive = false;

        if (item.TryGetProperty("value", out var valueElement))
        {
          value = DeserializeValue(valueElement);
        }

        if (item.TryGetProperty("isSensitive", out var sensitiveElement)
            && sensitiveElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
          isSensitive = sensitiveElement.GetBoolean();
        }

        items[property.Name] = new VarlockResolvedItem(property.Name, value, isSensitive);
      }
    }

    var sources = new List<VarlockSourceInfo>();
    if (root.TryGetProperty("sources", out var sourcesElement) && sourcesElement.ValueKind == JsonValueKind.Array)
    {
      foreach (var sourceElement in sourcesElement.EnumerateArray())
      {
        var label = sourceElement.TryGetProperty("label", out var labelElement)
          ? labelElement.GetString() ?? string.Empty
          : string.Empty;
        var enabled = sourceElement.TryGetProperty("enabled", out var enabledElement)
          && enabledElement.ValueKind is JsonValueKind.True or JsonValueKind.False
          && enabledElement.GetBoolean();
        var path = sourceElement.TryGetProperty("path", out var pathElement) && pathElement.ValueKind == JsonValueKind.String
          ? pathElement.GetString()
          : null;

        sources.Add(new VarlockSourceInfo(label, enabled, path));
      }
    }

    var redactLogs = true;
    var preventLeaks = true;
    if (root.TryGetProperty("settings", out var settingsElement) && settingsElement.ValueKind == JsonValueKind.Object)
    {
      if (settingsElement.TryGetProperty("redactLogs", out var redactLogsElement)
          && redactLogsElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
      {
        redactLogs = redactLogsElement.GetBoolean();
      }

      if (settingsElement.TryGetProperty("preventLeaks", out var preventLeaksElement)
          && preventLeaksElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
      {
        preventLeaks = preventLeaksElement.GetBoolean();
      }
    }

    var basePath = root.TryGetProperty("basePath", out var basePathElement) && basePathElement.ValueKind == JsonValueKind.String
      ? basePathElement.GetString()
      : null;

    return new VarlockResolvedGraph(items, sources, redactLogs, preventLeaks, basePath, contractVersion);
  }

  private static object? DeserializeValue(JsonElement element)
  {
    return element.ValueKind switch
    {
      JsonValueKind.Null => null,
      JsonValueKind.String => element.GetString(),
      JsonValueKind.True => true,
      JsonValueKind.False => false,
      JsonValueKind.Number => DeserializeNumber(element),
      JsonValueKind.Object => DeserializeObject(element),
      JsonValueKind.Array => DeserializeArray(element),
      _ => element.GetRawText(),
    };
  }

  private static object DeserializeNumber(JsonElement element)
  {
    if (element.TryGetInt64(out var int64Value))
    {
      return int64Value;
    }

    if (element.TryGetDecimal(out var decimalValue))
    {
      return decimalValue;
    }

    return element.GetDouble();
  }

  private static Dictionary<string, object?> DeserializeObject(JsonElement element)
  {
    var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
    foreach (var property in element.EnumerateObject())
    {
      dictionary[property.Name] = DeserializeValue(property.Value);
    }

    return dictionary;
  }

  private static List<object?> DeserializeArray(JsonElement element)
  {
    var list = new List<object?>();
    foreach (var item in element.EnumerateArray())
    {
      list.Add(DeserializeValue(item));
    }

    return list;
  }

  private static string QuoteArgument(string argument)
  {
    if (argument.Length == 0)
    {
      return "\"\"";
    }

    if (!argument.Any(ch => char.IsWhiteSpace(ch) || ch == '"'))
    {
      return argument;
    }

    var builder = new StringBuilder(argument.Length + 2);
    builder.Append('"');
    foreach (var ch in argument)
    {
      if (ch == '"')
      {
        builder.Append('\\');
      }

      builder.Append(ch);
    }

    builder.Append('"');
    return builder.ToString();
  }

  private static ProcessStartInfo CreateProcessStartInfo(
    string executablePath,
    string arguments,
    string workingDirectory)
  {
    var fileName = executablePath;
    var resolvedArguments = arguments;

    if (IsWindows() && string.Equals(Path.GetExtension(executablePath), ".js", StringComparison.OrdinalIgnoreCase))
    {
      fileName = "node";
      resolvedArguments = string.IsNullOrWhiteSpace(arguments)
        ? QuoteArgument(executablePath)
        : string.Join(" ", QuoteArgument(executablePath), arguments);
    }

    return new ProcessStartInfo
    {
      FileName = fileName,
      Arguments = resolvedArguments,
      WorkingDirectory = workingDirectory,
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true,
    };
  }

  private sealed class ProcessResult
  {
    public ProcessResult(string standardOutput, string standardError, int exitCode)
    {
      StandardOutput = standardOutput;
      StandardError = standardError;
      ExitCode = exitCode;
    }

    public string StandardOutput { get; }

    public string StandardError { get; }

    public int ExitCode { get; }
  }

  internal static string ResolveExecutable(VarlockLoadOptions options)
  {
    var workingDirectory = options.GetWorkingDirectory();

    if (!string.IsNullOrWhiteSpace(options.ExecutablePath))
    {
      return ResolveExplicitExecutable(options.ExecutablePath!, workingDirectory);
    }

    if (options.EnableLocalExecutableLookup)
    {
      var localPackageExecutable = FindNodeModulesPackageExecutable(workingDirectory);
      if (localPackageExecutable is not null)
      {
        return localPackageExecutable;
      }

      var localBinExecutable = FindNodeModulesExecutable(workingDirectory);
      if (localBinExecutable is not null)
      {
        return localBinExecutable;
      }

      var developmentExecutable = FindDevelopmentExecutable(workingDirectory);
      if (developmentExecutable is not null)
      {
        return developmentExecutable;
      }
    }

    if (options.EnablePathLookup)
    {
      var pathExecutable = FindPathExecutable(ExecutableName);
      if (pathExecutable is not null)
      {
        return pathExecutable;
      }
    }

    throw new VarlockBridgeException(
      VarlockBridgeErrorCategory.ExecutableNotFound,
      options.EnablePathLookup
        ? "Unable to locate a Varlock executable. Configure ExecutablePath, install a local package copy, or make 'varlock' discoverable from PATH."
        : "Unable to locate a Varlock executable. Configure ExecutablePath or enable a local/package-managed lookup path.");
  }

  private static string ResolveExplicitExecutable(string executablePath, string workingDirectory)
  {
    var resolved = Path.GetFullPath(Path.IsPathRooted(executablePath)
      ? executablePath
      : Path.Combine(workingDirectory, executablePath));

    if (File.Exists(resolved))
    {
      return resolved;
    }

    throw new VarlockBridgeException(
      VarlockBridgeErrorCategory.ExecutableNotFound,
      $"Unable to locate the configured Varlock executable at '{resolved}'.",
      filePath: resolved);
  }

  private static string? FindNodeModulesPackageExecutable(string workingDirectory)
  {
    var directory = new DirectoryInfo(workingDirectory);
    while (directory is not null)
    {
      var binDirectory = Path.Combine(directory.FullName, "node_modules", ExecutableName, "bin");
      var executable = FindExecutableInBinDirectory(binDirectory);
      if (executable is not null)
      {
        return executable;
      }

      directory = directory.Parent;
    }

    return null;
  }

  private static string? FindNodeModulesExecutable(string workingDirectory)
  {
    var directory = new DirectoryInfo(workingDirectory);
    while (directory is not null)
    {
      var binDirectory = Path.Combine(directory.FullName, "node_modules", ".bin");
      var executable = FindExecutableInDirectory(binDirectory, ExecutableName);
      if (executable is not null)
      {
        return executable;
      }

      directory = directory.Parent;
    }

    return null;
  }

  private static string? FindDevelopmentExecutable(string workingDirectory)
  {
    foreach (var searchRoot in EnumerateSearchRoots(workingDirectory))
    {
      var binDirectory = Path.Combine(searchRoot, "packages", ExecutableName, "bin");
      var executable = FindExecutableInBinDirectory(binDirectory);
      if (executable is not null)
      {
        return executable;
      }
    }

    return null;
  }

  private static IEnumerable<string> EnumerateSearchRoots(string workingDirectory)
  {
    var seen = new HashSet<string>(StringComparer.Ordinal);

    var directory = new DirectoryInfo(workingDirectory);
    while (directory is not null)
    {
      if (seen.Add(directory.FullName))
      {
        yield return directory.FullName;
      }

      directory = directory.Parent;
    }

    var assemblyDirectory = Path.GetDirectoryName(typeof(VarlockCliRuntime).GetTypeInfo().Assembly.Location);
    if (!string.IsNullOrWhiteSpace(assemblyDirectory))
    {
      directory = new DirectoryInfo(assemblyDirectory);
      while (directory is not null)
      {
        if (seen.Add(directory.FullName))
        {
          yield return directory.FullName;
        }

        directory = directory.Parent;
      }
    }
  }

  private static string? FindPathExecutable(string executableName)
  {
    var pathValue = Environment.GetEnvironmentVariable("PATH");
    if (string.IsNullOrWhiteSpace(pathValue))
    {
      return null;
    }

    foreach (var directory in pathValue.Split(Path.PathSeparator).Where(part => !string.IsNullOrWhiteSpace(part)))
    {
      var executable = FindExecutableInDirectory(directory, executableName);
      if (executable is not null)
      {
        return executable;
      }
    }

    return null;
  }

  private static string? FindExecutableInBinDirectory(string binDirectory)
  {
    if (!Directory.Exists(binDirectory))
    {
      return null;
    }

    if (IsWindows())
    {
      // On Windows, prefer .cmd wrapper over .js script
      var cmdPath = Path.Combine(binDirectory, "cli.cmd");
      if (File.Exists(cmdPath))
      {
        return cmdPath;
      }
    }

    // On all platforms, fall back to .js script
    var jsPath = Path.Combine(binDirectory, "cli.js");
    if (File.Exists(jsPath))
    {
      return jsPath;
    }

    return null;
  }

  private static string? FindExecutableInDirectory(string directory, string executableName)
  {
    if (!Directory.Exists(directory))
    {
      return null;
    }

    if (IsWindows())
    {
      // On Windows, prefer .cmd wrapper, then others
      var cmdPath = Path.Combine(directory, executableName + ".cmd");
      if (File.Exists(cmdPath))
      {
        return cmdPath;
      }

      foreach (var extension in WindowsExecutableExtensions)
      {
        if (extension == ".cmd")
        {
          continue; // Already checked above
        }

        var withExtension = Path.Combine(directory, executableName + extension);
        if (File.Exists(withExtension))
        {
          return withExtension;
        }
      }
    }

    var directPath = Path.Combine(directory, executableName);
    return File.Exists(directPath) ? directPath : null;
  }

  private static ProcessResult RunProcess(
    string executablePath,
    string arguments,
    string workingDirectory,
    IReadOnlyDictionary<string, string?>? environmentVariables,
    VarlockBridgeErrorCategory startupFailureCategory,
    string startupFailureMessage)
  {
    var processStartInfo = CreateProcessStartInfo(executablePath, arguments, workingDirectory);

    ApplyEnvironment(processStartInfo, environmentVariables);

    try
    {
      using var process = new Process { StartInfo = processStartInfo };
      process.Start();

      var standardOutput = process.StandardOutput.ReadToEnd();
      var standardError = process.StandardError.ReadToEnd();

      process.WaitForExit();

      return new ProcessResult(standardOutput, standardError, process.ExitCode);
    }
    catch (Win32Exception ex)
    {
      throw new VarlockBridgeException(
        startupFailureCategory,
        startupFailureMessage,
        ex,
        filePath: executablePath);
    }
  }

  private static bool IsWindows()
  {
    var platform = Environment.OSVersion.Platform;
    return platform == PlatformID.Win32NT
      || platform == PlatformID.Win32S
      || platform == PlatformID.Win32Windows
      || platform == PlatformID.WinCE;
  }
}
