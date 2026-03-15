using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DotnetWorkerNet8;

public sealed class Worker : BackgroundService
{
  private readonly IOptionsMonitor<VarlockWorkerOptions> _monitor;
  private readonly WorkerProofOptions _proofOptions;
  private readonly IHostEnvironment _environment;
  private readonly IHostApplicationLifetime _applicationLifetime;

  public Worker(
    IOptionsMonitor<VarlockWorkerOptions> monitor,
    WorkerProofOptions proofOptions,
    IHostEnvironment environment,
    IHostApplicationLifetime applicationLifetime)
  {
    _monitor = monitor;
    _proofOptions = proofOptions;
    _environment = environment;
    _applicationLifetime = applicationLifetime;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    if (_proofOptions.ReloadProof)
    {
      await RunReloadProofAsync(stoppingToken);
      return;
    }

    if (_proofOptions.ReloadFailProof)
    {
      await RunReloadFailureProofAsync(stoppingToken);
      return;
    }

    while (!stoppingToken.IsCancellationRequested)
    {
      await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }
  }

  private async Task RunReloadProofAsync(CancellationToken stoppingToken)
  {
    var reloadCount = 0;
    var reloadTaskSource = new TaskCompletionSource<WorkerConfigSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);

    using var subscription = _monitor.OnChange((options) =>
    {
      Interlocked.Increment(ref reloadCount);
      reloadTaskSource.TrySetResult(WorkerConfigSnapshot.From(options));
    });

    Console.WriteLine("WORKER_RELOAD_PROOF_INITIAL:" + JsonSerializer.Serialize(WorkerConfigSnapshot.From(_monitor.CurrentValue)));
    Console.Out.Flush();

    var schemaPath = Path.Combine(_environment.ContentRootPath, ".env.schema");
    var originalContent = await File.ReadAllTextAsync(schemaPath, stoppingToken);
    var modifiedContent = originalContent.Replace(
      "APP_NAME=varlock-worker",
      "APP_NAME=varlock-worker-reloaded",
      StringComparison.Ordinal);

    await File.WriteAllTextAsync(schemaPath, modifiedContent, stoppingToken);

    try
    {
      var completedTask = await Task.WhenAny(
        reloadTaskSource.Task,
        Task.Delay(TimeSpan.FromSeconds(15), stoppingToken));

      if (completedTask == reloadTaskSource.Task)
      {
        await Task.Delay(150, stoppingToken);
        var reloadedSnapshot = await reloadTaskSource.Task;
        Console.WriteLine("WORKER_RELOAD_PROOF_RELOADED:" + JsonSerializer.Serialize(reloadedSnapshot));
        Console.WriteLine("WORKER_RELOAD_PROOF_COUNT:" + reloadCount);
        Console.WriteLine("WORKER_RELOAD_PROOF_MONITOR_APP_NAME:" + _monitor.CurrentValue.APP_NAME);
      }
      else
      {
        Console.WriteLine("WORKER_RELOAD_PROOF_TIMEOUT");
      }
    }
    finally
    {
      await File.WriteAllTextAsync(schemaPath, originalContent, stoppingToken);
      _applicationLifetime.StopApplication();
    }
  }

  private async Task RunReloadFailureProofAsync(CancellationToken stoppingToken)
  {
    var reloadCount = 0;

    using var subscription = _monitor.OnChange(_ =>
    {
      Interlocked.Increment(ref reloadCount);
    });

    Console.WriteLine("WORKER_RELOAD_FAIL_PROOF_INITIAL:" + JsonSerializer.Serialize(WorkerConfigSnapshot.From(_monitor.CurrentValue)));
    Console.Out.Flush();

    var schemaPath = Path.Combine(_environment.ContentRootPath, ".env.schema");
    var originalContent = await File.ReadAllTextAsync(schemaPath, stoppingToken);
    await File.WriteAllTextAsync(schemaPath, "BROKEN_SYNTAX{{{not-a-valid-schema", stoppingToken);

    try
    {
      await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
      Console.WriteLine("WORKER_RELOAD_FAIL_PROOF_AFTER:" + JsonSerializer.Serialize(WorkerConfigSnapshot.From(_monitor.CurrentValue)));
      Console.WriteLine("WORKER_RELOAD_FAIL_PROOF_COUNT:" + reloadCount);
      Console.WriteLine("WORKER_RELOAD_FAIL_PROOF_MONITOR_APP_NAME:" + _monitor.CurrentValue.APP_NAME);
    }
    finally
    {
      await File.WriteAllTextAsync(schemaPath, originalContent, stoppingToken);
      _applicationLifetime.StopApplication();
    }
  }
}
