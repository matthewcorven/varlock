namespace dotnet_worker;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService // 👈 Varlock: inject IConfiguration
{
    // 👈 Varlock: known keys from .env.schema
    private static readonly string[] VarlockKeys = ["APP_NAME", "APP_PORT", "FEATURE_ENABLED"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 👈 Varlock: log configuration values on startup
        logger.LogInformation("Varlock configuration:");
        foreach (var key in VarlockKeys)
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
