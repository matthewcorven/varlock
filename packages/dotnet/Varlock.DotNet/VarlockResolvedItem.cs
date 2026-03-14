namespace Varlock.DotNet;

public sealed class VarlockResolvedItem
{
  public VarlockResolvedItem(string key, object? value, bool isSensitive)
  {
    Key = key;
    Value = value;
    IsSensitive = isSensitive;
  }

  public string Key { get; }

  public object? Value { get; }

  public bool IsSensitive { get; }
}