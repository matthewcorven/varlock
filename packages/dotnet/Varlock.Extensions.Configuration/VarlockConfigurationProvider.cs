using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Varlock.DotNet;

namespace Varlock.Extensions.Configuration;

public sealed class VarlockConfigurationProvider : ConfigurationProvider
{
  private readonly IVarlockRuntime _runtime;
  private readonly VarlockLoadOptions _loadOptions;

  public VarlockConfigurationProvider(
    VarlockConfigurationSource source,
    IVarlockRuntime runtime,
    VarlockLoadOptions loadOptions)
  {
    Source = source;
    _runtime = runtime;
    _loadOptions = loadOptions;
  }

  public VarlockConfigurationSource Source { get; }

  public override void Load()
  {
    try
    {
      var graph = _runtime.Load(_loadOptions);
      Data = new Dictionary<string, string?>(VarlockConfigurationFlattener.Flatten(graph));
    }
    catch (VarlockBridgeException ex) when (Source.Optional && ex.Category == VarlockBridgeErrorCategory.SchemaMissing)
    {
      Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }
  }
}