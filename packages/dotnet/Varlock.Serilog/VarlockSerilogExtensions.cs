using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Varlock.DotNet;

namespace Varlock.Serilog;

public static class VarlockSerilogExtensions
{
  public static LoggerConfiguration WithVarlockRedaction(this LoggerDestructuringConfiguration destructure, VarlockResolvedGraph graph)
  {
    if (destructure is null)
    {
      throw new ArgumentNullException(nameof(destructure));
    }

    if (graph is null)
    {
      throw new ArgumentNullException(nameof(graph));
    }

    return destructure.With(new VarlockRedactionPolicy(graph));
  }

  public static LoggerConfiguration WithVarlockMetadata(this LoggerEnrichmentConfiguration enrich, VarlockResolvedGraph graph)
  {
    if (enrich is null)
    {
      throw new ArgumentNullException(nameof(enrich));
    }

    if (graph is null)
    {
      throw new ArgumentNullException(nameof(graph));
    }

    return enrich.With(new VarlockMetadataEnricher(graph.RedactLogs));
  }

  private sealed class VarlockMetadataEnricher : ILogEventEnricher
  {
    private readonly bool _redactLogs;

    public VarlockMetadataEnricher(bool redactLogs)
    {
      _redactLogs = redactLogs;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
      if (logEvent is null)
      {
        throw new ArgumentNullException(nameof(logEvent));
      }

      if (propertyFactory is null)
      {
        throw new ArgumentNullException(nameof(propertyFactory));
      }

      logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("VarlockRedactLogs", _redactLogs));
    }
  }

  private sealed class VarlockRedactionPolicy : IDestructuringPolicy
  {
    private readonly HashSet<string> _sensitiveKeys;

    public VarlockRedactionPolicy(VarlockResolvedGraph graph)
    {
      _sensitiveKeys = new HashSet<string>(
        graph.Items
          .Where(item => item.Value.IsSensitive)
          .Select(item => item.Key),
        StringComparer.Ordinal);
    }

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
      if (propertyValueFactory is null)
      {
        throw new ArgumentNullException(nameof(propertyValueFactory));
      }

      if (TryDestructureDictionary(value, propertyValueFactory, out result))
      {
        return true;
      }

      if (value is IEnumerable && value is not string)
      {
        result = null!;
        return false;
      }

      var type = value.GetType();
      if (IsScalarLike(type))
      {
        result = null!;
        return false;
      }

      var properties = type
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
        .ToArray();

      if (properties.Length == 0)
      {
        result = null!;
        return false;
      }

      var logEventProperties = new List<LogEventProperty>(properties.Length);
      foreach (var property in properties)
      {
        logEventProperties.Add(new LogEventProperty(
          property.Name,
          CreatePropertyValue(property.Name, property.GetValue(value), propertyValueFactory)));
      }

      result = new StructureValue(logEventProperties);
      return true;
    }

    private static bool IsScalarLike(Type type)
    {
      return type.IsPrimitive
        || type.IsEnum
        || type == typeof(string)
        || type == typeof(decimal)
        || type == typeof(DateTime)
        || type == typeof(DateTimeOffset)
        || type == typeof(TimeSpan)
        || type == typeof(Guid)
        || type == typeof(Uri)
        || type == typeof(Version)
        || type == typeof(Type)
        || typeof(Exception).IsAssignableFrom(type)
        || typeof(Delegate).IsAssignableFrom(type);
    }

    private LogEventPropertyValue CreatePropertyValue(
      string propertyName,
      object? value,
      ILogEventPropertyValueFactory propertyValueFactory)
    {
      return _sensitiveKeys.Contains(propertyName)
        ? new ScalarValue("[REDACTED]")
        : value is null
          ? new ScalarValue(null)
          : propertyValueFactory.CreatePropertyValue(value, destructureObjects: true);
    }

    private bool TryDestructureDictionary(
      object value,
      ILogEventPropertyValueFactory propertyValueFactory,
      out LogEventPropertyValue result)
    {
      if (value is IDictionary dictionary)
      {
        var elements = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>();
        foreach (DictionaryEntry entry in dictionary)
        {
          elements.Add(new KeyValuePair<ScalarValue, LogEventPropertyValue>(
            new ScalarValue(entry.Key),
            CreateDictionaryValue(entry.Key as string, entry.Value, propertyValueFactory)));
        }

        result = new DictionaryValue(elements);
        return true;
      }

      result = null!;
      return false;
    }

    private LogEventPropertyValue CreateDictionaryValue(
      string? propertyName,
      object? value,
      ILogEventPropertyValueFactory propertyValueFactory)
    {
      return propertyName is not null && _sensitiveKeys.Contains(propertyName)
        ? new ScalarValue("[REDACTED]")
        : value is null
          ? new ScalarValue(null)
          : propertyValueFactory.CreatePropertyValue(value, destructureObjects: true);
    }
  }
}
