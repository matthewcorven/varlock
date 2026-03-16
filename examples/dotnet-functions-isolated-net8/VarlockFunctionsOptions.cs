namespace DotnetFunctionsIsolatedNet8;

public sealed class VarlockFunctionsOptions
{
  public string AppName { get; set; } = string.Empty;
  public int AppPort { get; set; }
  public bool FeatureEnabled { get; set; }
}
