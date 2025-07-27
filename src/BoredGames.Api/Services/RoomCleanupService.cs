namespace BoredGames.Services;

public sealed class RoomCleanupService(ILogger<RoomCleanupService> logger, RoomManager roomManager)
    : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Room Cleanup Service is starting.");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        try {
            logger.LogInformation("Running background cleanup task.");
            roomManager.CleanupStaleResources();
        }
        catch (Exception ex) {
            logger.LogError(ex, "An error occurred during the background cleanup task.");
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Room Cleanup Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}