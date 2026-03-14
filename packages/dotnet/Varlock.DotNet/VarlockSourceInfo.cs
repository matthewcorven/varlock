namespace Varlock.DotNet;

public sealed class VarlockSourceInfo
{
  public VarlockSourceInfo(string label, bool enabled, string? path)
  {
    Label = label;
    Enabled = enabled;
    Path = path;
  }

  public string Label { get; }

  public bool Enabled { get; }

  public string? Path { get; }
}