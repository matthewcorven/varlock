using System;
using Microsoft.Extensions.Configuration;
using Varlock.DotNet;

namespace Varlock.Extensions.Configuration;

public sealed class VarlockConfigurationSource : IConfigurationSource
{
  public string SchemaPath { get; set; } = ".env.schema";

  public bool Optional { get; set; }

  public string? EnvironmentName { get; set; }

  public string? WorkingDirectory { get; set; }

  public string? ExecutablePath { get; set; }

  public bool EnableLocalExecutableLookup { get; set; } = true;

  public bool EnablePathLookup { get; set; } = true;

  public IVarlockRuntime? Runtime { get; set; }

  public IConfigurationProvider Build(IConfigurationBuilder builder)
  {
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    return new VarlockConfigurationProvider(this, Runtime ?? new VarlockCliRuntime(), CreateLoadOptions(builder));
  }

  internal VarlockLoadOptions CreateLoadOptions(IConfigurationBuilder builder)
  {
    var workingDirectory = WorkingDirectory;
    if (string.IsNullOrWhiteSpace(workingDirectory)
        && builder.Properties.TryGetValue("BasePath", out var basePath)
        && basePath is string builderBasePath
        && !string.IsNullOrWhiteSpace(builderBasePath))
    {
      workingDirectory = builderBasePath;
    }

    return new VarlockLoadOptions
    {
      SchemaPath = SchemaPath,
      EnvironmentName = EnvironmentName,
      WorkingDirectory = workingDirectory,
      ExecutablePath = ExecutablePath,
      EnableLocalExecutableLookup = EnableLocalExecutableLookup,
      EnablePathLookup = EnablePathLookup,
    };
  }
}