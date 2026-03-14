using System;
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

    return new AppConfigSnapshot(
      configuration["APP_NAME"] ?? string.Empty,
      configuration.GetValue<int>("APP_PORT"),
      configuration.GetValue<bool>("FEATURE_ENABLED"),
      configuration["APPSETTINGS_ONLY"] ?? string.Empty,
      !string.IsNullOrWhiteSpace(configuration["SECRET_TOKEN"]),
      configuration["USERSECRETS_ONLY"] ?? string.Empty);
  }
}