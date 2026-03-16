using System;
using System.Collections.Generic;
using DotnetBlazorServerNet8.Generated;
using Microsoft.Extensions.Configuration;

namespace DotnetBlazorServerNet8;

public sealed record BlazorConfigSnapshot(
  string AppName,
  int AppPort,
  bool FeatureEnabled)
{
  public static BlazorConfigSnapshot From(IConfiguration configuration)
  {
    ArgumentNullException.ThrowIfNull(configuration);

    var projectedValues = new Dictionary<string, string?>(StringComparer.Ordinal);
    foreach (var binding in BlazorConfigMetadata.PropertyBindings)
    {
      var value = configuration[binding.Key];
      if (value is null) continue;
      projectedValues[binding.PropertyName] = value;
    }

    var binderSource = new ConfigurationBuilder()
      .AddInMemoryCollection(projectedValues)
      .Build();
    var generated = binderSource.Get<BlazorConfig>() ?? new BlazorConfig();

    return new BlazorConfigSnapshot(
      generated.AppName,
      Convert.ToInt32(generated.AppPort),
      generated.FeatureEnabled);
  }
}
