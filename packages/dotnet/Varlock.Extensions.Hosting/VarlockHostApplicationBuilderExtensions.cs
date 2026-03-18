using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Varlock.DotNet;
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

    return builder.AddVarlock(_ => { });
  }

  public static HostApplicationBuilder AddVarlock(this HostApplicationBuilder builder, Action<VarlockConfigurationSource> configure)
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

    RegisterServices(builder.Services, source);

    return builder;
  }

  internal static void RegisterServices(IServiceCollection services, VarlockConfigurationSource source)
  {
    var runtime = source.Runtime ?? new VarlockCliRuntime();
    services.TryAddSingleton<IVarlockRuntime>(runtime);
    services.TryAddSingleton(sp =>
    {
      var resolvedRuntime = sp.GetRequiredService<IVarlockRuntime>();
      return resolvedRuntime.Load(new VarlockLoadOptions
      {
        SchemaPath = source.SchemaPath,
        EnvironmentName = source.EnvironmentName,
        WorkingDirectory = source.WorkingDirectory,
        ExecutablePath = source.ExecutablePath,
        EnableLocalExecutableLookup = source.EnableLocalExecutableLookup,
        EnablePathLookup = source.EnablePathLookup,
      });
    });
  }
}
