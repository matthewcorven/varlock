using System;

namespace Varlock.DotNet;

public static class VarlockRedactionHelper
{
  public static string Redact(VarlockResolvedGraph graph, string key, string value)
  {
    if (graph is null)
    {
      throw new ArgumentNullException(nameof(graph));
    }

    if (key is null)
    {
      throw new ArgumentNullException(nameof(key));
    }

    if (value is null)
    {
      throw new ArgumentNullException(nameof(value));
    }

    return graph.Items.TryGetValue(key, out var item) && item.IsSensitive
      ? "[REDACTED]"
      : value;
  }
}
