using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// HIGH-012: Background service that periodically processes GDPR data retention.
/// Runs on a configurable interval (default: once per day) to:
/// 1. Process pending user deletion requests
/// 2. Anonymize data for users whose retention period has expired
/// </summary>
public class DataRetentionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataRetentionBackgroundService> _logger;

    /// <summary>
    /// How often the background service runs. Default: every 24 hours.
    /// </summary>
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    /// <summary>
    /// Number of days for standard data retention. Default: 3 years (1095 days).
    /// After this period of inactivity, inactive user data is anonymized.
    /// </summary>
    private const int StandardRetentionDays = 1095;

    public DataRetentionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DataRetentionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "HIGH-012: Data retention background service started. " +
            "RunInterval: {RunInterval}, StandardRetentionDays: {RetentionDays}",
            RunInterval, StandardRetentionDays);

        // Wait 5 minutes after startup to allow the application to fully initialize
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRetentionCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                _logger.LogInformation("HIGH-012: Data retention background service is shutting down.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "HIGH-012: Data retention background service encountered an error. " +
                    "Will retry at next interval.");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task ProcessRetentionCycleAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HIGH-012: Starting data retention cycle");

        // Create a new scope for each cycle to get fresh scoped services
        using var scope = _serviceProvider.CreateScope();
        var retentionService = scope.ServiceProvider.GetRequiredService<IDataRetentionService>();

        // 1. Process pending deletion requests (GDPR Article 17 requests)
        stoppingToken.ThrowIfCancellationRequested();
        var deletionsProcessed = await retentionService.ProcessPendingDeletionRequestsAsync();

        // 2. Process expired retention (automatic cleanup of old inactive accounts)
        stoppingToken.ThrowIfCancellationRequested();
        var expiredProcessed = await retentionService.ProcessExpiredRetentionAsync(StandardRetentionDays);

        _logger.LogInformation(
            "HIGH-012: Data retention cycle completed - " +
            "DeletionRequestsProcessed: {Deletions}, ExpiredRetentionProcessed: {Expired}",
            deletionsProcessed, expiredProcessed);
    }
}
