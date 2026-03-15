namespace Varlock.Extensions.Configuration;

/// <summary>
/// Controls how the configuration provider handles a failed reload attempt.
/// </summary>
public enum VarlockReloadFailureBehavior
{
  /// <summary>
  /// On reload failure, preserve the last successfully loaded configuration
  /// and suppress the reload notification so consumers see no change.
  /// </summary>
  KeepLastKnownGood = 0,
}
