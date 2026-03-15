using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Varlock.DotNet;
using Varlock.Extensions.Configuration;
using Xunit;

namespace Varlock.DotNet.Tests;

public sealed class ReloadTests : IDisposable
{
  private readonly string _tempRoot;

  public ReloadTests()
  {
    _tempRoot = Path.Combine(Path.GetTempPath(), $"varlock-reload-tests-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempRoot);
  }

  public void Dispose()
  {
    if (Directory.Exists(_tempRoot))
    {
      Directory.Delete(_tempRoot, recursive: true);
    }
  }

  [Fact]
  public void Reload_swaps_data_and_fires_notification_on_success()
  {
    var callCount = 0;
    var runtime = new SequenceRuntime(
      MakeGraph(new Dictionary<string, string?> { ["FOO"] = "initial" }),
      MakeGraph(new Dictionary<string, string?> { ["FOO"] = "reloaded" }));

    var source = new VarlockConfigurationSource
    {
      ReloadOnChange = true,
      SchemaPath = ".env.schema",
      WorkingDirectory = _tempRoot,
      Runtime = runtime,
    };

    File.WriteAllText(Path.Combine(_tempRoot, ".env.schema"), "FOO=initial");

    var config = new ConfigurationBuilder().Add(source).Build();

    Assert.Equal("initial", config["FOO"]);

    var reloadEvent = new ManualResetEventSlim(false);
    config.GetReloadToken().RegisterChangeCallback(_ =>
    {
      Interlocked.Increment(ref callCount);
      reloadEvent.Set();
    }, state: null);

    File.WriteAllText(Path.Combine(_tempRoot, ".env.schema"), "FOO=reloaded");

    Assert.True(reloadEvent.Wait(TimeSpan.FromSeconds(5)), "Reload notification should fire after file change.");
    Assert.Equal("reloaded", config["FOO"]);
    Assert.Equal(1, callCount);

    DisposeProviders(config);
  }

  [Fact]
  public void Reload_preserves_last_known_good_on_failure()
  {
    var runtime = new SequenceRuntime(
      MakeGraph(new Dictionary<string, string?> { ["FOO"] = "good" }),
      new VarlockBridgeException(VarlockBridgeErrorCategory.ResolutionFailed, "bad reload"));

    var source = new VarlockConfigurationSource
    {
      ReloadOnChange = true,
      SchemaPath = ".env.schema",
      WorkingDirectory = _tempRoot,
      Runtime = runtime,
    };

    File.WriteAllText(Path.Combine(_tempRoot, ".env.schema"), "FOO=good");

    var config = new ConfigurationBuilder().Add(source).Build();

    Assert.Equal("good", config["FOO"]);

    var notified = false;
    config.GetReloadToken().RegisterChangeCallback(_ => notified = true, state: null);

    File.WriteAllText(Path.Combine(_tempRoot, ".env.schema"), "FOO=bad");

    // Wait enough for debounce + reload attempt to complete.
    // 300ms debounce + 500ms buffer
    Thread.Sleep(800);

    Assert.Equal("good", config["FOO"]);
    Assert.False(notified, "Failed reload must not fire a reload notification.");

    DisposeProviders(config);
  }

  [Fact]
  public void Optional_reload_starts_empty_then_activates_on_file_appearance()
  {
    var schemaPath = Path.Combine(_tempRoot, ".env.schema");
    var runtime = new SequenceRuntime(
      new VarlockBridgeException(VarlockBridgeErrorCategory.SchemaMissing, "No schema"),
      MakeGraph(new Dictionary<string, string?> { ["FOO"] = "appeared" }));

    var source = new VarlockConfigurationSource
    {
      Optional = true,
      ReloadOnChange = true,
      SchemaPath = ".env.schema",
      WorkingDirectory = _tempRoot,
      Runtime = runtime,
    };

    var config = new ConfigurationBuilder().Add(source).Build();

    Assert.Null(config["FOO"]);
    Assert.Empty(config.GetChildren().ToArray());

    var reloadEvent = new ManualResetEventSlim(false);
    config.GetReloadToken().RegisterChangeCallback(_ => reloadEvent.Set(), state: null);

    File.WriteAllText(schemaPath, "FOO=appeared");

    Assert.True(reloadEvent.Wait(TimeSpan.FromSeconds(5)), "Optional schema appearance should trigger reload.");
    Assert.Equal("appeared", config["FOO"]);

    DisposeProviders(config);
  }

  [Fact]
  public void Reload_not_enabled_by_default()
  {
    var source = new VarlockConfigurationSource();
    Assert.False(source.ReloadOnChange);
    Assert.Equal(VarlockReloadFailureBehavior.KeepLastKnownGood, source.ReloadFailureBehavior);
  }

  [Fact]
  public void Multiple_rapid_changes_are_debounced_into_single_reload()
  {
    var runtime = new CountingRuntime(
      MakeGraph(new Dictionary<string, string?> { ["FOO"] = "v1" }),
      MakeGraph(new Dictionary<string, string?> { ["FOO"] = "v2" }));

    var source = new VarlockConfigurationSource
    {
      ReloadOnChange = true,
      SchemaPath = ".env.schema",
      WorkingDirectory = _tempRoot,
      Runtime = runtime,
    };

    File.WriteAllText(Path.Combine(_tempRoot, ".env.schema"), "FOO=v1");

    var config = new ConfigurationBuilder().Add(source).Build();

    Assert.Equal(1, runtime.LoadCount);

    var reloadEvent = new ManualResetEventSlim(false);
    config.GetReloadToken().RegisterChangeCallback(_ => reloadEvent.Set(), state: null);

    // Fire multiple rapid changes.
    for (var i = 0; i < 5; i++)
    {
      File.WriteAllText(Path.Combine(_tempRoot, ".env.schema"), $"FOO=v2-{i}");
      Thread.Sleep(50);
    }

    Assert.True(reloadEvent.Wait(TimeSpan.FromSeconds(5)), "Debounced reload should fire.");
    Assert.Equal(2, runtime.LoadCount);

    DisposeProviders(config);
  }

  [Fact]
  public void Failed_reload_does_not_recompute_watch_set()
  {
    // After a failed reload the watcher set should remain unchanged.
    // We verify indirectly: a subsequent successful change should
    // still trigger reload on the original watched files.
    var runtime = new SequenceRuntime(
      MakeGraph(new Dictionary<string, string?> { ["FOO"] = "initial" }),
      new VarlockBridgeException(VarlockBridgeErrorCategory.ResolutionFailed, "fail"),
      MakeGraph(new Dictionary<string, string?> { ["FOO"] = "recovered" }));

    var source = new VarlockConfigurationSource
    {
      ReloadOnChange = true,
      SchemaPath = ".env.schema",
      WorkingDirectory = _tempRoot,
      Runtime = runtime,
    };

    File.WriteAllText(Path.Combine(_tempRoot, ".env.schema"), "FOO=initial");

    var config = new ConfigurationBuilder().Add(source).Build();

    Assert.Equal("initial", config["FOO"]);

    // Trigger failed reload.
    File.WriteAllText(Path.Combine(_tempRoot, ".env.schema"), "FOO=fail");
    // 300ms debounce + 500ms buffer
    Thread.Sleep(800);
    Assert.Equal("initial", config["FOO"]);

    // Trigger a second change that should succeed.
    var reloadEvent = new ManualResetEventSlim(false);
    config.GetReloadToken().RegisterChangeCallback(_ => reloadEvent.Set(), state: null);
    File.WriteAllText(Path.Combine(_tempRoot, ".env.schema"), "FOO=recovered");

    Assert.True(reloadEvent.Wait(TimeSpan.FromSeconds(5)), "Watch set should remain intact after failed reload.");
    Assert.Equal("recovered", config["FOO"]);

    DisposeProviders(config);
  }

  private static VarlockResolvedGraph MakeGraph(IDictionary<string, string?> values)
  {
    var items = new Dictionary<string, VarlockResolvedItem>();
    foreach (var kv in values)
    {
      items[kv.Key] = new VarlockResolvedItem(kv.Key, kv.Value, isSensitive: false);
    }

    return new VarlockResolvedGraph(
      items,
      new List<VarlockSourceInfo>
      {
        new VarlockSourceInfo(".env.schema", enabled: true, path: ".env.schema"),
      },
      redactLogs: false,
      preventLeaks: false,
      basePath: null,
      contractVersion: 1);
  }

  private static void DisposeProviders(IConfigurationRoot config)
  {
    foreach (var provider in config.Providers)
    {
      if (provider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  private sealed class SequenceRuntime : IVarlockRuntime
  {
    private readonly object[] _sequence;
    private int _index;

    public SequenceRuntime(params object[] sequence)
    {
      _sequence = sequence;
    }

    public VarlockResolvedGraph Load(VarlockLoadOptions options)
    {
      var idx = Interlocked.Increment(ref _index) - 1;
      var item = idx < _sequence.Length ? _sequence[idx] : _sequence[_sequence.Length - 1];
      if (item is VarlockBridgeException ex)
      {
        throw ex;
      }

      return (VarlockResolvedGraph)item;
    }

    public Task<VarlockResolvedGraph> LoadAsync(VarlockLoadOptions options, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Load(options));
    }
  }

  private sealed class CountingRuntime : IVarlockRuntime
  {
    private readonly object[] _sequence;
    private int _index;

    public int LoadCount => _index;

    public CountingRuntime(params object[] sequence)
    {
      _sequence = sequence;
    }

    public VarlockResolvedGraph Load(VarlockLoadOptions options)
    {
      var idx = Interlocked.Increment(ref _index) - 1;
      var item = idx < _sequence.Length ? _sequence[idx] : _sequence[_sequence.Length - 1];
      if (item is VarlockBridgeException ex)
      {
        throw ex;
      }

      return (VarlockResolvedGraph)item;
    }

    public Task<VarlockResolvedGraph> LoadAsync(VarlockLoadOptions options, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Load(options));
    }
  }
}
