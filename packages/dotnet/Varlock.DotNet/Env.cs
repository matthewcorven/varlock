using System;
using System.Threading;
using System.Threading.Tasks;

namespace Varlock.DotNet;

public static class Env
{
  public static VarlockResolvedGraph Load() => Load(new VarlockLoadOptions());

  public static VarlockResolvedGraph Load(VarlockLoadOptions options) =>
    new VarlockCliRuntime().Load(options);

  public static VarlockResolvedGraph Load(Action<VarlockLoadOptions> configure)
  {
    var options = new VarlockLoadOptions();
    configure(options);
    return new VarlockCliRuntime().Load(options);
  }

  public static Task<VarlockResolvedGraph> LoadAsync(CancellationToken cancellationToken = default) =>
    LoadAsync(new VarlockLoadOptions(), cancellationToken);

  public static Task<VarlockResolvedGraph> LoadAsync(VarlockLoadOptions options, CancellationToken cancellationToken = default) =>
    new VarlockCliRuntime().LoadAsync(options, cancellationToken);
}
