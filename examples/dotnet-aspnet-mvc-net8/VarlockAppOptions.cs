namespace DotnetAspNetMvcNet8;

/// <summary>
/// Bindable options class whose property names match the Varlock
/// configuration keys exactly.  Used in the reload proof to
/// demonstrate <c>IOptionsMonitor&lt;T&gt;.OnChange</c>.
/// </summary>
public sealed class VarlockAppOptions
{
  // Property names intentionally match the UPPER_SNAKE_CASE
  // keys from .env.schema so the default configuration binder
  // maps them without additional attributes.
  public string APP_NAME { get; set; } = string.Empty;

  public int APP_PORT { get; set; }

  public bool FEATURE_ENABLED { get; set; }
}
