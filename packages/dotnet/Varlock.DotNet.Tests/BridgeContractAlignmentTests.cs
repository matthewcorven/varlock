using System.Collections.Generic;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Varlock.Extensions.Configuration;
using Xunit;

namespace Varlock.DotNet.Tests;

public sealed class BridgeContractAlignmentTests
{
  private const string FixtureCliVersion = "0.0.0-test";
  private const string FixtureWorkingDirectory = "/repo";

  [Fact]
  public void ParseCliOutput_reads_shared_success_fixture()
  {
    var payload = ReadSharedBridgeFixture("success.json");

    var graph = VarlockCliRuntime.ParseCliOutput(payload, standardError: string.Empty, exitCode: 0);

    Assert.Equal(1, graph.ContractVersion);
    Assert.Equal(FixtureWorkingDirectory, graph.BasePath);
    Assert.True(graph.RedactLogs);
    Assert.True(graph.PreventLeaks);
    Assert.Collection(
      graph.Sources,
      source =>
      {
        Assert.Equal($"directory - {FixtureWorkingDirectory}", source.Label);
        Assert.True(source.Enabled);
        Assert.Null(source.Path);
      },
      source =>
      {
        Assert.Equal(".env.schema", source.Label);
        Assert.True(source.Enabled);
        Assert.Equal(".env.schema", source.Path);
      });
    Assert.Equal("bar", graph.Items["FOO"].Value);
    Assert.True(graph.Items["FOO"].IsSensitive);
  }

  [Theory]
  [InlineData("schema-missing.json", VarlockBridgeErrorCategory.SchemaMissing, "No .env or .env.schema files found")]
  [InlineData("schema-invalid.json", VarlockBridgeErrorCategory.SchemaInvalid, "only true, false, or `inferFromPrefix()` is allowed for @defaultSensitive decorator")]
  [InlineData("resolution-failed.json", VarlockBridgeErrorCategory.ResolutionFailed, "Value is required but is currently empty")]
  [InlineData("plugin-load-failed.json", VarlockBridgeErrorCategory.PluginLoadFailed, "Bad @plugin path: ../../../env-graph/test/plugins/test-plugin-no-package-json")]
  public void ParseCliOutput_maps_shared_failure_fixture_to_typed_exception(
    string fixtureFileName,
    VarlockBridgeErrorCategory expectedCategory,
    string expectedMessage)
  {
    var payload = ReadSharedBridgeFixture(fixtureFileName);

    var exception = Assert.Throws<VarlockBridgeException>(() =>
      VarlockCliRuntime.ParseCliOutput(payload, standardError: "stderr", exitCode: 1));

    Assert.Equal(expectedCategory, exception.Category);
    Assert.Equal(expectedMessage, exception.Message);
    Assert.Equal(1, exception.ExitCode);
    Assert.Equal(payload, exception.StandardOutput);
    Assert.Equal("stderr", exception.StandardError);
    Assert.Null(exception.FilePath);
    Assert.Null(exception.Line);
    Assert.Null(exception.Column);
  }

  [Fact]
  public void ParseCliOutput_maps_shared_location_fixture_to_typed_exception()
  {
    var payload = ReadSharedBridgeFixture("schema-invalid-location.json");

    var exception = Assert.Throws<VarlockBridgeException>(() =>
      VarlockCliRuntime.ParseCliOutput(payload, standardError: "stderr", exitCode: 1));

    Assert.Equal(VarlockBridgeErrorCategory.SchemaInvalid, exception.Category);
    Assert.Equal("Parse error: Expected \"#\" or [ \\t] but \"F\" found.", exception.Message);
    Assert.Equal(1, exception.ExitCode);
    Assert.Equal(".env.schema", exception.FilePath);
    Assert.Equal(3, exception.Line);
    Assert.Equal(1, exception.Column);
    Assert.Equal(payload, exception.StandardOutput);
    Assert.Equal("stderr", exception.StandardError);
  }

  [Fact]
  public void EnsureExecutableSupportsBridgeContract_accepts_supported_version_mismatch_probe()
  {
    var payload = ReadSharedBridgeFixture("executable-version-mismatch.json")
      .Replace("\"2\"", "\"0\"", StringComparison.Ordinal);

    VarlockCliRuntime.EnsureExecutableSupportsBridgeContract(
      payload,
      standardError: string.Empty,
      exitCode: 1,
      executablePath: "/repo/packages/varlock/bin/cli.js");
  }

  [Fact]
  public void EnsureExecutableSupportsBridgeContract_rejects_incompatible_probe()
  {
    const string payload = """
      {
        "contractVersion": 1,
        "cliVersion": "0.0.0-test",
        "command": "load",
        "format": "json-full",
        "ok": false,
        "category": "executable-version-mismatch",
        "message": "Requested bridge contract version \"0\" is not supported by this varlock executable",
        "requestedContractVersion": "0",
        "supportedContractVersion": 2
      }
      """;

    var exception = Assert.Throws<VarlockBridgeException>(() =>
      VarlockCliRuntime.EnsureExecutableSupportsBridgeContract(
        payload,
        standardError: string.Empty,
        exitCode: 1,
        executablePath: "/repo/packages/varlock/bin/cli.js"));

    Assert.Equal(VarlockBridgeErrorCategory.ExecutableVersionMismatch, exception.Category);
    Assert.Contains("bridge contract version 2", exception.Message);
  }

  [Fact]
  public void ResolveExecutable_prefers_package_local_then_local_bin_then_repo_local_before_path()
  {
    var root = Path.Combine(Path.GetTempPath(), $"varlock-dotnet-tests-{System.Guid.NewGuid():N}");

    try
    {
      var workingDirectory = Path.Combine(root, "app");
      Directory.CreateDirectory(workingDirectory);

      var packageExecutable = Path.Combine(root, "node_modules", "varlock", "bin", "cli.js");
      Directory.CreateDirectory(Path.GetDirectoryName(packageExecutable)!);
      File.WriteAllText(packageExecutable, string.Empty);

      var localBinExecutable = Path.Combine(root, "node_modules", ".bin", "varlock");
      Directory.CreateDirectory(Path.GetDirectoryName(localBinExecutable)!);
      File.WriteAllText(localBinExecutable, string.Empty);

      var repoExecutable = Path.Combine(root, "packages", "varlock", "bin", "cli.js");
      Directory.CreateDirectory(Path.GetDirectoryName(repoExecutable)!);
      File.WriteAllText(repoExecutable, string.Empty);

      var resolved = VarlockCliRuntime.ResolveExecutable(new VarlockLoadOptions
      {
        WorkingDirectory = workingDirectory,
        EnablePathLookup = false,
      });

      Assert.Equal(packageExecutable, resolved);

      File.Delete(packageExecutable);

      resolved = VarlockCliRuntime.ResolveExecutable(new VarlockLoadOptions
      {
        WorkingDirectory = workingDirectory,
        EnablePathLookup = false,
      });

      Assert.Equal(localBinExecutable, resolved);

      File.Delete(localBinExecutable);

      resolved = VarlockCliRuntime.ResolveExecutable(new VarlockLoadOptions
      {
        WorkingDirectory = workingDirectory,
        EnablePathLookup = false,
      });

      Assert.Equal(repoExecutable, resolved);
    }
    finally
    {
      if (Directory.Exists(root))
      {
        Directory.Delete(root, recursive: true);
      }
    }
  }

  [Fact]
  public void ResolveExecutable_uses_path_only_when_explicitly_enabled()
  {
    var root = Path.Combine(Path.GetTempPath(), $"varlock-dotnet-tests-{Guid.NewGuid():N}");
    var originalPath = Environment.GetEnvironmentVariable("PATH");

    try
    {
      var workingDirectory = Path.Combine(root, "app");
      Directory.CreateDirectory(workingDirectory);

      var pathDirectory = Path.Combine(root, "path-bin");
      Directory.CreateDirectory(pathDirectory);

      var pathExecutable = Path.Combine(pathDirectory, "varlock");
      File.WriteAllText(pathExecutable, string.Empty);

      Environment.SetEnvironmentVariable(
        "PATH",
        string.IsNullOrWhiteSpace(originalPath)
          ? pathDirectory
          : string.Join(Path.PathSeparator, pathDirectory, originalPath));

      var resolved = VarlockCliRuntime.ResolveExecutable(new VarlockLoadOptions
      {
        WorkingDirectory = workingDirectory,
        EnableLocalExecutableLookup = false,
        EnablePathLookup = true,
      });

      Assert.Equal(pathExecutable, resolved);

      var exception = Assert.Throws<VarlockBridgeException>(() => VarlockCliRuntime.ResolveExecutable(new VarlockLoadOptions
      {
        WorkingDirectory = workingDirectory,
        EnableLocalExecutableLookup = false,
        EnablePathLookup = false,
      }));

      Assert.Equal(VarlockBridgeErrorCategory.ExecutableNotFound, exception.Category);
    }
    finally
    {
      Environment.SetEnvironmentVariable("PATH", originalPath);

      if (Directory.Exists(root))
      {
        Directory.Delete(root, recursive: true);
      }
    }
  }

  [Fact]
  public void AddVarlock_allows_optional_schema_missing_failures()
  {
    var configuration = new ConfigurationBuilder()
      .AddVarlock(source =>
      {
        source.Optional = true;
        source.Runtime = new ThrowingRuntime(new VarlockBridgeException(
          VarlockBridgeErrorCategory.SchemaMissing,
          "No .env or .env.schema files found"));
      })
      .Build();

    Assert.Null(configuration["FOO"]);
    Assert.Empty(configuration.GetChildren());
  }

  private sealed class ThrowingRuntime : IVarlockRuntime
  {
    private readonly VarlockBridgeException _exception;

    public ThrowingRuntime(VarlockBridgeException exception)
    {
      _exception = exception;
    }

    public VarlockResolvedGraph Load(VarlockLoadOptions options)
    {
      throw _exception;
    }

    public System.Threading.Tasks.Task<VarlockResolvedGraph> LoadAsync(
      VarlockLoadOptions options,
      System.Threading.CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      throw _exception;
    }
  }

  private static string ReadSharedBridgeFixture(string fixtureFileName)
  {
    var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureFileName);
    var payload = File.ReadAllText(fixturePath);

    return payload
      .Replace("__CLI_VERSION__", FixtureCliVersion, StringComparison.Ordinal)
      .Replace("__CWD__", FixtureWorkingDirectory, StringComparison.Ordinal);
  }
}