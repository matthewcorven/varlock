using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Varlock.DotNet;

namespace Varlock.Extensions.Configuration;

internal static class VarlockConfigurationFlattener
{
  public static IDictionary<string, string?> Flatten(VarlockResolvedGraph graph)
  {
    var flattened = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    foreach (var item in graph.Items.Values)
    {
      AddValue(flattened, item.Key, item.Value);
    }

    return flattened;
  }

  private static void AddValue(IDictionary<string, string?> flattened, string key, object? value)
  {
    switch (value)
    {
      case null:
        flattened[key] = null;
        return;
      case string stringValue:
        flattened[key] = stringValue;
        return;
      case bool boolValue:
        flattened[key] = boolValue ? bool.TrueString : bool.FalseString;
        return;
      case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
        flattened[key] = Convert.ToString(value, CultureInfo.InvariantCulture);
        return;
      case IReadOnlyDictionary<string, object?> readOnlyDictionary:
        foreach (var pair in readOnlyDictionary)
        {
          AddValue(flattened, ConfigurationPath.Combine(key, pair.Key), pair.Value);
        }

        return;
      case IDictionary<string, object?> dictionary:
        foreach (var pair in dictionary)
        {
          AddValue(flattened, ConfigurationPath.Combine(key, pair.Key), pair.Value);
        }

        return;
      case IEnumerable enumerable when value is not string:
        var index = 0;
        foreach (var item in enumerable)
        {
          AddValue(flattened, ConfigurationPath.Combine(key, index.ToString(CultureInfo.InvariantCulture)), item);
          index++;
        }

        return;
      default:
        flattened[key] = Convert.ToString(value, CultureInfo.InvariantCulture);
        return;
    }
  }
}