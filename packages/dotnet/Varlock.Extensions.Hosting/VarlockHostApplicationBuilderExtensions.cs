using System;
using Microsoft.Extensions.Hosting;
using Varlock.Extensions.Configuration;

namespace Varlock.Extensions.Hosting;

public static class VarlockHostApplicationBuilderExtensions
{
  public static HostApplicationBuilder AddVarlock(this HostApplicationBuilder builder)
  {
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    builder.Configuration.AddVarlock();
    return builder;
  }

  public static HostApplicationBuilder AddVarlock(this HostApplicationBuilder builder, Action<VarlockConfigurationSource> configure)
  {
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    builder.Configuration.AddVarlock(configure);
    return builder;
  }
}
