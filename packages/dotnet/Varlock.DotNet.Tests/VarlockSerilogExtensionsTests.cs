using System;
using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Varlock.DotNet;
using Varlock.Serilog;
using Xunit;

namespace Varlock.DotNet.Tests;

public sealed class VarlockSerilogExtensionsTests
{
  [Fact]
  public void WithVarlockRedaction_redacts_sensitive_properties_during_destructuring_even_when_redact_logs_is_false()
  {
    var graph = MakeGraph(
      redactLogs: false,
      (Key: "SECRET_TOKEN", IsSensitive: true),
      (Key: "APP_NAME", IsSensitive: false));
    var sink = new CollectingSink();

    using var logger = new LoggerConfiguration()
      .Destructure.WithVarlockRedaction(graph)
      .WriteTo.Sink(sink)
      .CreateLogger();

    logger.Information("{@Config}", new
    {
      SECRET_TOKEN = "super-secret",
      APP_NAME = "varlock-console",
    });

    var evt = Assert.Single(sink.Events);
    var config = Assert.IsType<StructureValue>(evt.Properties["Config"]);

    Assert.Equal("[REDACTED]", GetScalarPropertyValue(config, "SECRET_TOKEN"));
    Assert.Equal("varlock-console", GetScalarPropertyValue(config, "APP_NAME"));
  }

  [Fact]
  public void WithVarlockRedaction_leaves_values_unchanged_when_graph_has_no_sensitive_keys()
  {
    var graph = MakeGraph(redactLogs: true, (Key: "APP_NAME", IsSensitive: false));
    var sink = new CollectingSink();

    using var logger = new LoggerConfiguration()
      .Destructure.WithVarlockRedaction(graph)
      .WriteTo.Sink(sink)
      .CreateLogger();

    logger.Information("{@Config}", new
    {
      SECRET_TOKEN = "super-secret",
      APP_NAME = "varlock-console",
    });

    var evt = Assert.Single(sink.Events);
    var config = Assert.IsType<StructureValue>(evt.Properties["Config"]);

    Assert.Equal("super-secret", GetScalarPropertyValue(config, "SECRET_TOKEN"));
    Assert.Equal("varlock-console", GetScalarPropertyValue(config, "APP_NAME"));
  }

  [Fact]
  public void WithVarlockRedaction_does_not_intercept_scalar_message_template_parameters()
  {
    var graph = MakeGraph(redactLogs: true, (Key: "SECRET_TOKEN", IsSensitive: true));
    var sink = new CollectingSink();

    using var logger = new LoggerConfiguration()
      .Destructure.WithVarlockRedaction(graph)
      .WriteTo.Sink(sink)
      .CreateLogger();

    logger.Information("{SECRET_TOKEN}", "super-secret");

    var evt = Assert.Single(sink.Events);
    var value = Assert.IsType<ScalarValue>(evt.Properties["SECRET_TOKEN"]);

    Assert.Equal("super-secret", value.Value);
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void WithVarlockMetadata_adds_redact_logs_metadata_without_redacting_values(bool redactLogs)
  {
    var graph = MakeGraph(redactLogs, (Key: "SECRET_TOKEN", IsSensitive: true));
    var sink = new CollectingSink();

    using var logger = new LoggerConfiguration()
      .Enrich.WithVarlockMetadata(graph)
      .WriteTo.Sink(sink)
      .CreateLogger();

    logger.Information("{@Config}", new
    {
      SECRET_TOKEN = "super-secret",
    });

    var evt = Assert.Single(sink.Events);
    var config = Assert.IsType<StructureValue>(evt.Properties["Config"]);
    var redactLogsProperty = Assert.IsType<ScalarValue>(evt.Properties["VarlockRedactLogs"]);

    Assert.Equal("super-secret", GetScalarPropertyValue(config, "SECRET_TOKEN"));
    Assert.Equal(redactLogs, Assert.IsType<bool>(redactLogsProperty.Value));
  }

  [Theory]
  [InlineData("SECRET_TOKEN", "super-secret", "[REDACTED]")]
  [InlineData("APP_NAME", "varlock-console", "varlock-console")]
  [InlineData("MISSING_KEY", "missing-value", "missing-value")]
  [InlineData("secret_token", "case-mismatch", "case-mismatch")]
  public void VarlockRedactionHelper_redacts_only_exact_sensitive_key_matches(
    string key,
    string value,
    string expected)
  {
    var graph = MakeGraph(
      redactLogs: true,
      (Key: "SECRET_TOKEN", IsSensitive: true),
      (Key: "APP_NAME", IsSensitive: false));

    Assert.Equal(expected, VarlockRedactionHelper.Redact(graph, key, value));
  }

  private static VarlockResolvedGraph MakeGraph(bool redactLogs, params (string Key, bool IsSensitive)[] items)
  {
    var graphItems = new Dictionary<string, VarlockResolvedItem>(StringComparer.Ordinal);
    foreach (var item in items)
    {
      graphItems[item.Key] = new VarlockResolvedItem(item.Key, value: null, item.IsSensitive);
    }

    return new VarlockResolvedGraph(
      graphItems,
      Array.Empty<VarlockSourceInfo>(),
      redactLogs,
      preventLeaks: false,
      basePath: null,
      contractVersion: 1);
  }

  private static string? GetScalarPropertyValue(StructureValue structure, string name)
  {
    var property = Assert.Single(structure.Properties, item => item.Name == name);
    return Assert.IsType<ScalarValue>(property.Value).Value as string;
  }

  private sealed class CollectingSink : ILogEventSink
  {
    public List<LogEvent> Events { get; } = new();

    public void Emit(LogEvent logEvent)
    {
      Events.Add(logEvent);
    }
  }
}
