using System.Threading;
using System.Threading.Tasks;

namespace Varlock.DotNet;

public interface IVarlockRuntime
{
  VarlockResolvedGraph Load(VarlockLoadOptions options);

  Task<VarlockResolvedGraph> LoadAsync(VarlockLoadOptions options, CancellationToken cancellationToken = default);
}