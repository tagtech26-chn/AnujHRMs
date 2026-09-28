using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<AttendanceWorker>();

var host = builder.Build();

await host.RunAsync();

public sealed class AttendanceWorker(ILogger<AttendanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AnujHRMS Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("AnujHRMS Worker heartbeat: {UtcNow}", DateTime.UtcNow);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
