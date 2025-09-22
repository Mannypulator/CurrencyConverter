using System;
using CurrencyConverter.API.Application.Abstractions;

namespace CurrencyConverter.API.BackgroundServices;

public class CurrencyRateUpdaterService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CurrencyRateUpdaterService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(15); 
    private readonly TimeSpan _historicalUpdateInterval = TimeSpan.FromHours(24); 
    private DateTime _lastHistoricalUpdate = DateTime.MinValue;

    public CurrencyRateUpdaterService(IServiceProvider serviceProvider, ILogger<CurrencyRateUpdaterService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Currency Rate Updater Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var rateUpdateService = scope.ServiceProvider.GetRequiredService<IRateUpdateService>();

               
                await UpdateRealTimeRates(rateUpdateService, stoppingToken);

             
                if (DateTime.UtcNow - _lastHistoricalUpdate > _historicalUpdateInterval)
                {
                    await UpdateHistoricalRates(rateUpdateService, stoppingToken);
                    _lastHistoricalUpdate = DateTime.UtcNow;
                }

                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Currency Rate Updater Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Currency Rate Updater Service");

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task UpdateRealTimeRates(IRateUpdateService rateUpdateService, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Starting real-time rates update");

            var shouldUpdate = await rateUpdateService.ShouldUpdateRatesAsync(cancellationToken);
            if (shouldUpdate)
            {
                await rateUpdateService.UpdateRealTimeRatesAsync(cancellationToken);
                _logger.LogInformation("Real-time rates updated successfully");
            }
            else
            {
                _logger.LogDebug("Real-time rates are up to date, skipping update");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update real-time rates");
        }
    }

    private async Task UpdateHistoricalRates(IRateUpdateService rateUpdateService, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Starting historical rates update");

          
            var endDate = DateTime.UtcNow.Date.AddDays(-1); 
            var startDate = endDate.AddDays(-30);

            await rateUpdateService.UpdateHistoricalRatesAsync(startDate, endDate, cancellationToken);
            _logger.LogInformation("Historical rates updated successfully for period {StartDate} to {EndDate}",
                startDate.ToShortDateString(), endDate.ToShortDateString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update historical rates");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Currency Rate Updater Service is stopping");
        await base.StopAsync(stoppingToken);
    }
}

