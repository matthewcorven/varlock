namespace DotnetWorkerNet8;

public sealed class VarlockWorkerOptions
{
  public string APP_NAME { get; set; } = string.Empty;

  public int APP_PORT { get; set; }

  public bool FEATURE_ENABLED { get; set; }
}
