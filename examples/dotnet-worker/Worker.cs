namespace dotnet_worker;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService // 👈 Varlock: inject IConfiguration
{
    private static readonly string[] Keys = ["APP_NAME", "WORKER_MESSAGE"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker started with Varlock configuration:");
        foreach (var key in Keys)
        {
            logger.LogInformation("  {Key} = {Value}", key, configuration[key] ?? "(null)");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
