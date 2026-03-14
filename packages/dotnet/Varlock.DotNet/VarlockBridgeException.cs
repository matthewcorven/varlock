using System;

namespace Varlock.DotNet;

public sealed class VarlockBridgeException : Exception
{
  public VarlockBridgeException(
    VarlockBridgeErrorCategory category,
    string message,
    Exception? innerException = null,
    int? exitCode = null,
    string? standardOutput = null,
    string? standardError = null,
    string? filePath = null,
    int? line = null,
    int? column = null)
    : base(message, innerException)
  {
    Category = category;
    ExitCode = exitCode;
    StandardOutput = standardOutput;
    StandardError = standardError;
    FilePath = filePath;
    Line = line;
    Column = column;
  }

  public VarlockBridgeErrorCategory Category { get; }

  public int? ExitCode { get; }

  public string? StandardOutput { get; }

  public string? StandardError { get; }

  public string? FilePath { get; }

  public int? Line { get; }

  public int? Column { get; }
}