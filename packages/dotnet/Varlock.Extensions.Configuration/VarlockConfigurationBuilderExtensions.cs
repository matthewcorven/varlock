using System;
using Microsoft.Extensions.Configuration;

namespace Varlock.Extensions.Configuration;

public static class VarlockConfigurationBuilderExtensions
{
  public static IConfigurationBuilder AddVarlock(this IConfigurationBuilder builder)
  {
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    return builder.AddVarlock(_ => { });
  }

  public static IConfigurationBuilder AddVarlock(this IConfigurationBuilder builder, Action<VarlockConfigurationSource> configure)
  {
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    if (configure is null)
    {
      throw new ArgumentNullException(nameof(configure));
    }

    var source = new VarlockConfigurationSource();
    configure(source);

    builder.Add(source);
    return builder;
  }
}