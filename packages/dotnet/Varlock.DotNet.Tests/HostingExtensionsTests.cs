using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Varlock.DotNet;
using Varlock.Extensions.Configuration;
using Varlock.Extensions.Hosting;
using Xunit;

namespace Varlock.DotNet.Tests;

public sealed class HostingExtensionsTests
{
  [Fact]
  public void AddVarlock_parameterless_uses_current_directory_defaults()
  {
    var originalCurrentDirectory = Environment.CurrentDirectory;

    try
    {
      Environment.CurrentDirectory = Path.Combine(TestPaths.RepositoryRoot, "examples", "dotnet-console-net8");

      var builder = new HostApplicationBuilder();

      builder.AddVarlock();

      Assert.Equal("varlock-console", builder.Configuration["APP_NAME"]);
      Assert.Equal("4310", builder.Configuration["HTTP_PORT"]);
      Assert.True(bool.Parse(builder.Configuration["FEATURE_ENABLED"]!));

      var source = Assert.Single(builder.Configuration.Sources.OfType<VarlockConfigurationSource>());
      Assert.False(source.ReloadOnChange);
    }
    finally
    {
      Environment.CurrentDirectory = originalCurrentDirectory;
    }
  }

  [Fact]
  public void AddVarlock_leaves_working_directory_unset_when_not_configured()
  {
    var runtime = new RecordingRuntime();
    var contentRoot = TestPaths.CreateTempDirectory("varlock-hosting-tests");
    Directory.CreateDirectory(contentRoot);

    try
    {
      var builder = new HostApplicationBuilder();

      builder.AddVarlock((source) =>
      {
        source.Runtime = runtime;
        source.SchemaPath = "proof.env.schema";
      });

      Assert.Null(runtime.LastOptions?.WorkingDirectory);
      Assert.Equal("bar", builder.Configuration["FOO"]);
    }
    finally
    {
      Directory.Delete(contentRoot, recursive: true);
    }
  }

  [Fact]
  public void AddVarlock_preserves_explicit_working_directory()
  {
    var runtime = new RecordingRuntime();
    var contentRoot = TestPaths.CreateTempDirectory("varlock-hosting-tests");
    var explicitWorkingDirectory = Path.Combine(contentRoot, "explicit-working-directory");
    Directory.CreateDirectory(explicitWorkingDirectory);

    try
    {
      var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
      {
        ContentRootPath = contentRoot,
      });

      builder.AddVarlock((source) =>
      {
        source.Runtime = runtime;
        source.WorkingDirectory = explicitWorkingDirectory;
      });

      Assert.Equal(Path.GetFullPath(explicitWorkingDirectory), runtime.LastOptions?.WorkingDirectory);
      Assert.Equal("bar", builder.Configuration["FOO"]);
    }
    finally
    {
      Directory.Delete(contentRoot, recursive: true);
    }
  }

  [Fact]
  public void Hosting_package_exposes_exactly_two_hostapplicationbuilder_addvarlock_overloads()
  {
    var methods = typeof(VarlockHostApplicationBuilderExtensions)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(method => method.Name == nameof(VarlockHostApplicationBuilderExtensions.AddVarlock))
      .OrderBy(method => method.GetParameters().Length)
      .ToArray();

    Assert.Collection(
      methods,
      method =>
      {
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(HostApplicationBuilder), parameters[0].ParameterType);
      },
      method =>
      {
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(HostApplicationBuilder), parameters[0].ParameterType);
        Assert.Equal(typeof(Action<VarlockConfigurationSource>), parameters[1].ParameterType);
      });
  }

  private sealed class RecordingRuntime : IVarlockRuntime
  {
    public VarlockLoadOptions? LastOptions { get; private set; }

    public VarlockResolvedGraph Load(VarlockLoadOptions options)
    {
      LastOptions = new VarlockLoadOptions
      {
        SchemaPath = options.SchemaPath,
        WorkingDirectory = options.WorkingDirectory,
        EnvironmentName = options.EnvironmentName,
        ExecutablePath = options.ExecutablePath,
        EnableLocalExecutableLookup = options.EnableLocalExecutableLookup,
        EnablePathLookup = options.EnablePathLookup,
        EnvironmentVariables = options.EnvironmentVariables,
      };

      return new VarlockResolvedGraph(
        new Dictionary<string, VarlockResolvedItem>
        {
          ["FOO"] = new VarlockResolvedItem("FOO", "bar", isSensitive: false),
        },
        Array.Empty<VarlockSourceInfo>(),
        redactLogs: false,
        preventLeaks: false,
        basePath: null,
        contractVersion: 1);
    }

    public Task<VarlockResolvedGraph> LoadAsync(VarlockLoadOptions options, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Load(options));
    }
  }
}
