using System;

namespace DotnetWorkerNet8;

public sealed record WorkerConfigSnapshot(
  string AppName,
  int AppPort,
  bool FeatureEnabled)
{
  public static WorkerConfigSnapshot From(VarlockWorkerOptions options)
  {
    if (options is null)
    {
      throw new ArgumentNullException(nameof(options));
    }

    return new WorkerConfigSnapshot(
      options.APP_NAME,
      options.APP_PORT,
      options.FEATURE_ENABLED);
  }
}
