using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;
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
    var root = TestPaths.CreateTempDirectory("varlock-dotnet-tests");

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
    var root = TestPaths.CreateTempDirectory("varlock-dotnet-tests");
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
  public void ResolveExecutable_on_windows_prefers_cmd_wrapper_over_js_script()
  {
    // This test validates that on Windows, the runtime looks for .cmd wrappers
    // in the bin directories before falling back to .js scripts.
    // This ensures that proof harnesses creating .cmd wrappers are found correctly.
    if (Environment.OSVersion.Platform != PlatformID.Win32NT)
    {
      // Skip on non-Windows platforms
      return;
    }

    var root = TestPaths.CreateTempDirectory("varlock-dotnet-tests");

    try
    {
      var workingDirectory = Path.Combine(root, "app");
      Directory.CreateDirectory(workingDirectory);

      // Create both .cmd and .js in package-local bin directory
      var packageBinDirectory = Path.Combine(root, "node_modules", "varlock", "bin");
      Directory.CreateDirectory(packageBinDirectory);

      var cmdPath = Path.Combine(packageBinDirectory, "cli.cmd");
      var jsPath = Path.Combine(packageBinDirectory, "cli.js");

      File.WriteAllText(cmdPath, "@echo off\r\necho test");
      File.WriteAllText(jsPath, "// test script");

      var resolved = VarlockCliRuntime.ResolveExecutable(new VarlockLoadOptions
      {
        WorkingDirectory = workingDirectory,
        EnablePathLookup = false,
      });

      // Should prefer .cmd over .js
      Assert.Equal(cmdPath, resolved);

      // Remove .cmd and verify it falls back to .js
      File.Delete(cmdPath);

      resolved = VarlockCliRuntime.ResolveExecutable(new VarlockLoadOptions
      {
        WorkingDirectory = workingDirectory,
        EnablePathLookup = false,
      });

      Assert.Equal(jsPath, resolved);
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
  public void Load_executes_repo_local_js_entrypoint_without_explicit_executable_path()
  {
    var root = TestPaths.CreateTempDirectory("varlock-dotnet-tests");

    try
    {
      var workingDirectory = Path.Combine(root, "app");
      Directory.CreateDirectory(workingDirectory);

      var schemaPath = Path.Combine(workingDirectory, ".env.schema");
      File.WriteAllText(schemaPath, "APP_NAME=repo-local-proof");

      var markerPath = Path.Combine(root, ".varlock-repo-local-proof");
      var repoExecutable = Path.Combine(root, "packages", "varlock", "bin", "cli.js");
      Directory.CreateDirectory(Path.GetDirectoryName(repoExecutable)!);

      var executableSource = """
        #!/usr/bin/env node
        import { writeFileSync } from 'node:fs';

        writeFileSync(__MARKER_PATH__, 'executed\n');

        const args = process.argv.slice(2);
        const bridgeContractIndex = args.indexOf('--bridge-contract');
        const bridgeContract = bridgeContractIndex >= 0 ? args[bridgeContractIndex + 1] : null;

        if (bridgeContract === '0') {
          console.log(JSON.stringify({
            contractVersion: 1,
            cliVersion: '0.0.0-test',
            command: 'load',
            format: 'json-full',
            ok: false,
            category: 'executable-version-mismatch',
            message: 'Requested bridge contract version "0" is not supported by this varlock executable',
            requestedContractVersion: '0',
            supportedContractVersion: 1,
          }));
          process.exit(1);
        }

        console.log(JSON.stringify({
          contractVersion: 1,
          cliVersion: '0.0.0-test',
          command: 'load',
          format: 'json-full',
          ok: true,
          graph: {
            basePath: __WORKING_DIRECTORY__,
            config: {
              APP_NAME: {
                value: 'repo-local-proof',
                isSensitive: false,
              },
            },
            sources: [
              {
                label: '.env.schema',
                enabled: true,
                path: '.env.schema',
              },
            ],
            settings: {
              redactLogs: true,
              preventLeaks: true,
            },
          },
        }));
        """
        .Replace("__MARKER_PATH__", JsonSerializer.Serialize(markerPath), StringComparison.Ordinal)
        .Replace("__WORKING_DIRECTORY__", JsonSerializer.Serialize(workingDirectory), StringComparison.Ordinal);

      File.WriteAllText(repoExecutable, executableSource);
      if (!OperatingSystem.IsWindows())
      {
        File.SetUnixFileMode(
          repoExecutable,
          UnixFileMode.UserRead
          | UnixFileMode.UserWrite
          | UnixFileMode.UserExecute
          | UnixFileMode.GroupRead
          | UnixFileMode.GroupExecute
          | UnixFileMode.OtherRead
          | UnixFileMode.OtherExecute);
      }

      var graph = new VarlockCliRuntime().Load(new VarlockLoadOptions
      {
        WorkingDirectory = workingDirectory,
        EnablePathLookup = false,
      });

      Assert.Equal(1, graph.ContractVersion);
      Assert.Equal(workingDirectory, graph.BasePath);
      Assert.Equal("repo-local-proof", graph.Items["APP_NAME"].Value);
      Assert.Contains(graph.Sources, source => source.Path == ".env.schema");
      Assert.True(File.Exists(markerPath));
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

  [Fact]
  public void ResolveExecutable_not_found_message_includes_searched_paths_and_install_suggestion()
  {
    var root = TestPaths.CreateTempDirectory("varlock-dotnet-tests");
    var originalPath = Environment.GetEnvironmentVariable("PATH");

    try
    {
      var workingDirectory = Path.Combine(root, "app");
      Directory.CreateDirectory(workingDirectory);

      // Clear PATH to an empty temp dir so PATH lookup finds nothing
      var emptyPathDir = Path.Combine(root, "empty-bin");
      Directory.CreateDirectory(emptyPathDir);
      Environment.SetEnvironmentVariable("PATH", emptyPathDir);

      var exception = Assert.Throws<VarlockBridgeException>(() =>
        VarlockCliRuntime.ResolveExecutable(new VarlockLoadOptions
        {
          WorkingDirectory = workingDirectory,
          EnableLocalExecutableLookup = false,
          EnablePathLookup = true,
        }));

      Assert.Equal(VarlockBridgeErrorCategory.ExecutableNotFound, exception.Category);
      Assert.Contains("Varlock CLI not found", exception.Message);
      Assert.Contains("PATH", exception.Message);
      Assert.Contains("npm install --save-dev varlock", exception.Message);
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
  public void ResolveExecutable_not_found_message_omits_local_paths_when_lookup_disabled()
  {
    var root = TestPaths.CreateTempDirectory("varlock-dotnet-tests");

    try
    {
      var workingDirectory = Path.Combine(root, "app");
      Directory.CreateDirectory(workingDirectory);

      var exception = Assert.Throws<VarlockBridgeException>(() =>
        VarlockCliRuntime.ResolveExecutable(new VarlockLoadOptions
        {
          WorkingDirectory = workingDirectory,
          EnableLocalExecutableLookup = false,
          EnablePathLookup = false,
        }));

      Assert.Equal(VarlockBridgeErrorCategory.ExecutableNotFound, exception.Category);
      Assert.DoesNotContain("node_modules", exception.Message);
      Assert.Contains("all lookup paths are disabled", exception.Message);
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
  public void ParseCliOutput_missing_payload_includes_stderr_when_available()
  {
    var exception = Assert.Throws<VarlockBridgeException>(() =>
      VarlockCliRuntime.ParseCliOutput(
        standardOutput: string.Empty,
        standardError: "Error: Cannot find module 'varlock'\n    at Function.resolve",
        exitCode: 1));

    Assert.Equal(VarlockBridgeErrorCategory.BridgeInternalError, exception.Category);
    Assert.Contains("exited with code 1", exception.Message);
    Assert.Contains("CLI stderr:", exception.Message);
    Assert.Contains("Cannot find module", exception.Message);
  }

  [Fact]
  public void ParseCliOutput_missing_payload_without_stderr_omits_stderr_section()
  {
    var exception = Assert.Throws<VarlockBridgeException>(() =>
      VarlockCliRuntime.ParseCliOutput(
        standardOutput: string.Empty,
        standardError: null,
        exitCode: 0));

    Assert.Equal(VarlockBridgeErrorCategory.BridgeInternalError, exception.Category);
    Assert.Contains("empty machine-readable payload", exception.Message);
    Assert.DoesNotContain("CLI stderr:", exception.Message);
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
