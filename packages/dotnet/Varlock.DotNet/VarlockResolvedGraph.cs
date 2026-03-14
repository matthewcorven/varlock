using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Varlock.DotNet;

public sealed class VarlockResolvedGraph
{
  public VarlockResolvedGraph(
    IDictionary<string, VarlockResolvedItem> items,
    IList<VarlockSourceInfo> sources,
    bool redactLogs,
    bool preventLeaks,
    string? basePath,
    int? contractVersion)
  {
    Items = new ReadOnlyDictionary<string, VarlockResolvedItem>(items);
    Sources = new ReadOnlyCollection<VarlockSourceInfo>(sources);
    RedactLogs = redactLogs;
    PreventLeaks = preventLeaks;
    BasePath = basePath;
    ContractVersion = contractVersion;
  }

  public IReadOnlyDictionary<string, VarlockResolvedItem> Items { get; }

  public IReadOnlyList<VarlockSourceInfo> Sources { get; }

  public bool RedactLogs { get; }

  public bool PreventLeaks { get; }

  public string? BasePath { get; }

  public int? ContractVersion { get; }
}