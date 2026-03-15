using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Varlock.DotNet;

namespace Varlock.Extensions.Configuration;

public sealed class VarlockConfigurationProvider : ConfigurationProvider, IDisposable
{
  /// <summary>
  /// Debounce window in milliseconds. Overlapping file-system events that
  /// arrive within this window are coalesced into a single reload cycle.
  /// Default is 300 ms.
  /// </summary>
  internal const int DebounceMilliseconds = 300;

  private readonly IVarlockRuntime _runtime;
  private readonly VarlockLoadOptions _loadOptions;
  private readonly object _reloadLock = new object();

  private Timer? _debounceTimer;
  private List<FileSystemWatcher>? _watchers;
  private HashSet<string>? _watchedPaths;
  private bool _disposed;

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

      if (Source.ReloadOnChange)
      {
        SetupWatchers(graph);
      }
    }
    catch (VarlockBridgeException ex) when (Source.Optional && ex.Category == VarlockBridgeErrorCategory.SchemaMissing)
    {
      Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

      if (Source.ReloadOnChange)
      {
        WatchForSchemaAppearance();
      }
    }
  }

  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    _disposed = true;
    _debounceTimer?.Dispose();
    _debounceTimer = null;
    DisposeWatchers();
  }

  private void SetupWatchers(VarlockResolvedGraph? graph)
  {
    lock (_reloadLock)
    {
      DisposeWatchers();

      var paths = ComputeWatchPaths(graph);
      _watchedPaths = paths;
      _watchers = new List<FileSystemWatcher>();

      var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var path in paths)
      {
        var directory = Path.GetDirectoryName(path);
        if (directory != null)
        {
          directories.Add(directory);
        }
      }

      foreach (var directory in directories)
      {
        if (!Directory.Exists(directory))
        {
          continue;
        }

        var watcher = new FileSystemWatcher(directory)
        {
          NotifyFilter = NotifyFilters.FileName
            | NotifyFilters.LastWrite
            | NotifyFilters.Size
            | NotifyFilters.CreationTime,
          IncludeSubdirectories = false,
          EnableRaisingEvents = true,
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileRenamed;

        _watchers.Add(watcher);
      }
    }
  }

  private void WatchForSchemaAppearance()
  {
    lock (_reloadLock)
    {
      DisposeWatchers();

      var schemaFullPath = _loadOptions.GetSchemaFullPath();
      var schemaDirectory = Path.GetDirectoryName(schemaFullPath);
      if (schemaDirectory == null || !Directory.Exists(schemaDirectory))
      {
        return;
      }

      _watchedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { schemaFullPath };
      _watchers = new List<FileSystemWatcher>();

      var watcher = new FileSystemWatcher(schemaDirectory)
      {
        NotifyFilter = NotifyFilters.FileName
          | NotifyFilters.LastWrite
          | NotifyFilters.Size
          | NotifyFilters.CreationTime,
        IncludeSubdirectories = false,
        EnableRaisingEvents = true,
      };

      watcher.Changed += OnFileChanged;
      watcher.Created += OnFileChanged;
      watcher.Renamed += OnFileRenamed;

      _watchers.Add(watcher);
    }
  }

  private HashSet<string> ComputeWatchPaths(VarlockResolvedGraph? graph)
  {
    var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    var schemaFullPath = _loadOptions.GetSchemaFullPath();
    paths.Add(schemaFullPath);

    if (graph?.Sources != null)
    {
      var basePath = graph.BasePath ?? _loadOptions.GetWorkingDirectory();
      foreach (var source in graph.Sources)
      {
        if (string.IsNullOrWhiteSpace(source.Path))
        {
          continue;
        }

        var sourcePath = Path.IsPathRooted(source.Path!)
          ? source.Path!
          : Path.GetFullPath(Path.Combine(basePath, source.Path!));

        paths.Add(sourcePath);
      }
    }

    return paths;
  }

  private void OnFileChanged(object sender, FileSystemEventArgs e)
  {
    if (_disposed)
    {
      return;
    }

    if (_watchedPaths != null && !_watchedPaths.Contains(e.FullPath))
    {
      return;
    }

    ScheduleReload();
  }

  private void OnFileRenamed(object sender, RenamedEventArgs e)
  {
    if (_disposed)
    {
      return;
    }

    var matchesOld = _watchedPaths != null && _watchedPaths.Contains(e.OldFullPath);
    var matchesNew = _watchedPaths != null && _watchedPaths.Contains(e.FullPath);

    if (matchesOld || matchesNew)
    {
      ScheduleReload();
    }
  }

  private void ScheduleReload()
  {
    if (_disposed)
    {
      return;
    }

    lock (_reloadLock)
    {
      if (_disposed)
      {
        return;
      }

      if (_debounceTimer != null)
      {
        _debounceTimer.Change(DebounceMilliseconds, Timeout.Infinite);
      }
      else
      {
        _debounceTimer = new Timer(ExecuteReload, state: null, DebounceMilliseconds, Timeout.Infinite);
      }
    }
  }

  private void ExecuteReload(object? state)
  {
    if (_disposed)
    {
      return;
    }

    lock (_reloadLock)
    {
      if (_disposed)
      {
        return;
      }

      try
      {
        var graph = _runtime.Load(_loadOptions);
        var newData = new Dictionary<string, string?>(VarlockConfigurationFlattener.Flatten(graph));

        // Atomic swap: set Data then notify.
        Data = newData;

        // Recompute watch set from the new graph.
        SetupWatchers(graph);

        // Fire reload notification only after successful commit.
        OnReload();
      }
      catch
      {
        // Failed reload: preserve last-known-good Data, do not fire
        // OnReload(), and do not recompute the watch set.
      }
    }
  }

  private void DisposeWatchers()
  {
    if (_watchers == null)
    {
      return;
    }

    foreach (var watcher in _watchers)
    {
      watcher.EnableRaisingEvents = false;
      watcher.Changed -= OnFileChanged;
      watcher.Created -= OnFileChanged;
      watcher.Deleted -= OnFileChanged;
      watcher.Renamed -= OnFileRenamed;
      watcher.Dispose();
    }

    _watchers = null;
  }
}