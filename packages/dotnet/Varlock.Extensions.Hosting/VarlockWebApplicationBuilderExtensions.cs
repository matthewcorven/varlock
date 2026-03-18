#if NET10_0_OR_GREATER
using System;
using Microsoft.AspNetCore.Builder;
using Varlock.Extensions.Configuration;

namespace Varlock.Extensions.Hosting;

public static class VarlockWebApplicationBuilderExtensions
{
  public static WebApplicationBuilder AddVarlock(this WebApplicationBuilder builder)
  {
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    builder.Configuration.AddVarlock();
    return builder;
  }

  public static WebApplicationBuilder AddVarlock(this WebApplicationBuilder builder, Action<VarlockConfigurationSource> configure)
  {
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    builder.Configuration.AddVarlock(configure);
    return builder;
  }
}
#endif
