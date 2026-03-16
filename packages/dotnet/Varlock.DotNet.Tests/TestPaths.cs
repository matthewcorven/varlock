using System;
using System.IO;

namespace Varlock.DotNet.Tests;

internal static class TestPaths
{
  internal static string RepositoryRoot { get; } = FindRepositoryRoot();

  internal static string CreateTempDirectory(string prefix)
  {
    var tempRoot = Path.Combine(RepositoryRoot, ".tmp", "dotnet-tests");
    Directory.CreateDirectory(tempRoot);

    var directory = Path.Combine(tempRoot, $"{prefix}-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    return directory;
  }

  private static string FindRepositoryRoot()
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      var examplesPath = Path.Combine(directory.FullName, "examples");
      var packagesPath = Path.Combine(directory.FullName, "packages");
      if (Directory.Exists(examplesPath) && Directory.Exists(packagesPath))
      {
        return directory.FullName;
      }

      directory = directory.Parent;
    }

    throw new InvalidOperationException("Unable to locate the repository root from the test output directory.");
  }
}
