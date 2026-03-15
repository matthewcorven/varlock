using System;
using System.Collections.Generic;
using DotnetAspNetMvcNet8.Generated;
using Microsoft.Extensions.Configuration;

namespace DotnetAspNetMvcNet8;

public sealed record AppConfigSnapshot(
  string AppName,
  int AppPort,
  bool FeatureEnabled,
  string AppSettingsOnly,
  bool SecretTokenPresent,
  string UserSecretsOnly)
{
  public static AppConfigSnapshot From(IConfiguration configuration)
  {
    ArgumentNullException.ThrowIfNull(configuration);

    var projectedValues = new Dictionary<string, string?>(StringComparer.Ordinal);
    foreach (var binding in AppConfigMetadata.PropertyBindings)
    {
      var value = configuration[binding.Key];
      if (value is null) continue;
      projectedValues[binding.PropertyName] = value;
    }

    var binderSource = new ConfigurationBuilder()
      .AddInMemoryCollection(projectedValues)
      .Build();
    var generated = binderSource.Get<AppConfig>() ?? new AppConfig();

    return new AppConfigSnapshot(
      generated.AppName,
      Convert.ToInt32(generated.AppPort),
      generated.FeatureEnabled,
      configuration["APPSETTINGS_ONLY"] ?? string.Empty,
      !string.IsNullOrWhiteSpace(generated.SecretToken),
      configuration["USERSECRETS_ONLY"] ?? string.Empty);
  }

  public static AppConfigSnapshot From(VarlockAppOptions options, IConfiguration configuration)
  {
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(configuration);

    return new AppConfigSnapshot(
      options.APP_NAME,
      options.APP_PORT,
      options.FEATURE_ENABLED,
      configuration["APPSETTINGS_ONLY"] ?? string.Empty,
      !string.IsNullOrWhiteSpace(configuration["SECRET_TOKEN"]),
      configuration["USERSECRETS_ONLY"] ?? string.Empty);
  }
}
