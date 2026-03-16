using System;
using System.Collections.Generic;
using DotnetFunctionsIsolatedNet8.Generated;
using Microsoft.Extensions.Configuration;

namespace DotnetFunctionsIsolatedNet8;

public sealed record FunctionsConfigSnapshot(
  string AppName,
  int AppPort,
  bool FeatureEnabled,
  string? FunctionsOnlyKey)
{
  public static FunctionsConfigSnapshot From(IConfiguration configuration)
  {
    ArgumentNullException.ThrowIfNull(configuration);

    var projectedValues = new Dictionary<string, string?>(StringComparer.Ordinal);
    foreach (var binding in FunctionsConfigMetadata.PropertyBindings)
    {
      var value = configuration[binding.Key];
      if (value is null) continue;
      projectedValues[binding.PropertyName] = value;
    }

    var binderSource = new ConfigurationBuilder()
      .AddInMemoryCollection(projectedValues)
      .Build();
    var generated = binderSource.Get<FunctionsConfig>() ?? new FunctionsConfig();

    return new FunctionsConfigSnapshot(
      generated.AppName,
      Convert.ToInt32(generated.AppPort),
      generated.FeatureEnabled,
      configuration["FUNCTIONS_ONLY_KEY"]);
  }
}
