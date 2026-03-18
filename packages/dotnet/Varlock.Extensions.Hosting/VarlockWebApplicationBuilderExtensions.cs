#if NET10_0_OR_GREATER
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Varlock.DotNet;
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

    return builder.AddVarlock(_ => { });
  }

  public static WebApplicationBuilder AddVarlock(this WebApplicationBuilder builder, Action<VarlockConfigurationSource> configure)
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
    ((IConfigurationBuilder)builder.Configuration).Add(source);

    VarlockHostApplicationBuilderExtensions.RegisterServices(builder.Services, source);

    return builder;
  }

  public static WebApplicationBuilder AddVarlock<TConfig>(this WebApplicationBuilder builder)
    where TConfig : class
  {
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    return builder.AddVarlock<TConfig>(_ => { });
  }

  public static WebApplicationBuilder AddVarlock<TConfig>(
    this WebApplicationBuilder builder,
    Action<VarlockConfigurationSource> configure)
    where TConfig : class
  {
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    if (configure is null)
    {
      throw new ArgumentNullException(nameof(configure));
    }

    builder.AddVarlock(configure);
    builder.Services.Configure<TConfig>(builder.Configuration);

    return builder;
  }
}
#endif
