using System;
using System.Collections.Generic;
using System.IO;

namespace Varlock.DotNet;

public sealed class VarlockLoadOptions
{
  public string SchemaPath { get; set; } = ".env.schema";

  public string? WorkingDirectory { get; set; }

  public string? EnvironmentName { get; set; }

  public string? ExecutablePath { get; set; }

  public bool EnableLocalExecutableLookup { get; set; } = true;

  public bool EnablePathLookup { get; set; } = true;

  public IReadOnlyDictionary<string, string?>? EnvironmentVariables { get; set; }

  public string GetWorkingDirectory()
  {
    var workingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory)
      ? Environment.CurrentDirectory
      : WorkingDirectory!;

    return Path.GetFullPath(workingDirectory);
  }

  public string GetSchemaFullPath()
  {
    var schemaPath = string.IsNullOrWhiteSpace(SchemaPath) ? ".env.schema" : SchemaPath;
    return Path.GetFullPath(Path.IsPathRooted(schemaPath)
      ? schemaPath
      : Path.Combine(GetWorkingDirectory(), schemaPath));
  }
}