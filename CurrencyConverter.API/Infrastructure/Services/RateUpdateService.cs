using System;
using CurrencyConverter.API.Application.Abstractions;
using CurrencyConverter.API.Domain.Entity;

namespace CurrencyConverter.API.Infrastructure.Services;

public class RateUpdateService : IRateUpdateService
{
    private readonly IExternalCurrencyService _externalService;
    private readonly IExchangeRateRepository _exchangeRateRepository;
    private readonly ICurrencyRepository _currencyRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RateUpdateService> _logger;

    private readonly string[] _majorCurrencies = { "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY" };
    private readonly string[] _baseCurrencies = { "USD", "EUR", "GBP" };

    public RateUpdateService(
        IExternalCurrencyService externalService,
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository,
        ICacheService cacheService,
        ILogger<RateUpdateService> logger)
    {
        _externalService = externalService;
        _exchangeRateRepository = exchangeRateRepository;
        _currencyRepository = currencyRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task UpdateRealTimeRatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting real-time rates update");

        var updatedRates = 0;
        var totalAttempts = 0;

        foreach (var baseCurrency in _baseCurrencies)
        {
            try
            {
                totalAttempts++;
                _logger.LogDebug("Fetching real-time rates for base currency: {BaseCurrency}", baseCurrency);

                var externalRates = await _externalService.GetRealTimeRatesAsync(baseCurrency, cancellationToken);

                if (externalRates?.Rates == null || !externalRates.Rates.Any())
                {
                    _logger.LogWarning("No rates received for base currency {BaseCurrency}", baseCurrency);
                    continue;
                }

                var ratesToAdd = new List<ExchangeRate>();
                var currentTime = DateTime.UtcNow;

                foreach (var rate in externalRates.Rates)
                {
                    // Validate target currency exists
                    if (!await _currencyRepository.CurrencyExistsAsync(rate.Key, cancellationToken))
                    {
                        _logger.LogDebug("Skipping rate for unknown currency: {Currency}", rate.Key);
                        continue;
                    }

                    // Check if rate already exists for today
                    var existingRate = await _exchangeRateRepository.GetRateAsync(baseCurrency, rate.Key, currentTime.Date, cancellationToken);

                    if (existingRate != null)
                    {
                        // Update existing rate if different
                        if (Math.Abs(existingRate.Rate - rate.Value) > 0.00000001m)
                        {
                            existingRate.Rate = rate.Value;
                            existingRate.UpdatedAt = currentTime;
                            await _exchangeRateRepository.UpdateRateAsync(existingRate, cancellationToken);
                            updatedRates++;
                        }
                    }
                    else
                    {
                        // Add new rate
                        var newRate = new ExchangeRate
                        {
                            BaseCurrencyCode = baseCurrency,
                            TargetCurrencyCode = rate.Key,
                            Rate = rate.Value,
                            Date = currentTime.Date,
                            IsHistorical = false,
                            CreatedAt = currentTime
                        };

                        ratesToAdd.Add(newRate);
                    }
                }

                if (ratesToAdd.Any())
                {
                    await _exchangeRateRepository.AddRatesAsync(ratesToAdd, cancellationToken);
                    updatedRates += ratesToAdd.Count;
                }

                // Clear cache for this base currency
                await ClearRateCache(baseCurrency, cancellationToken);

                _logger.LogDebug("Updated {Count} rates for base currency {BaseCurrency}", ratesToAdd.Count, baseCurrency);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update rates for base currency {BaseCurrency}", baseCurrency);
            }
        }

        _logger.LogInformation("Real-time rates update completed. Updated {UpdatedRates} rates from {TotalAttempts} base currencies",
            updatedRates, totalAttempts);
    }

    public async Task UpdateHistoricalRatesAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting historical rates update from {StartDate} to {EndDate}",
            startDate.ToShortDateString(), endDate.ToShortDateString());

        var updatedRates = 0;
        var totalAttempts = 0;

        // Generate currency pairs
        var currencyPairs = GenerateCurrencyPairs();

        foreach (var (baseCurrency, targetCurrency) in currencyPairs)
        {
            try
            {
                totalAttempts++;
                _logger.LogDebug("Fetching historical rates for {BaseCurrency}/{TargetCurrency}", baseCurrency, targetCurrency);

                var externalRates = await _externalService.GetHistoricalRatesAsync(
                    baseCurrency, targetCurrency, startDate, endDate, cancellationToken);

                if (externalRates?.Rates == null || !externalRates.Rates.Any())
                {
                    _logger.LogWarning("No historical rates received for {BaseCurrency}/{TargetCurrency}", baseCurrency, targetCurrency);
                    continue;
                }

                var ratesToAdd = new List<ExchangeRate>();

                foreach (var rate in externalRates.Rates)
                {
                    if (!DateTime.TryParse(rate.Key, out var rateDate))
                    {
                        _logger.LogWarning("Invalid date format in historical data: {Date}", rate.Key);
                        continue;
                    }

                    // Check if rate already exists
                    var existingRate = await _exchangeRateRepository.GetRateAsync(baseCurrency, targetCurrency, rateDate.Date, cancellationToken);

                    if (existingRate == null)
                    {
                        var newRate = new ExchangeRate
                        {
                            BaseCurrencyCode = baseCurrency,
                            TargetCurrencyCode = targetCurrency,
                            Rate = rate.Value,
                            Date = rateDate.Date,
                            IsHistorical = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        ratesToAdd.Add(newRate);
                    }
                }

                if (ratesToAdd.Any())
                {
                    await _exchangeRateRepository.AddRatesAsync(ratesToAdd, cancellationToken);
                    updatedRates += ratesToAdd.Count;
                    _logger.LogDebug("Added {Count} historical rates for {BaseCurrency}/{TargetCurrency}",
                        ratesToAdd.Count, baseCurrency, targetCurrency);
                }

                // Add delay between requests to avoid overwhelming the external service
                await Task.Delay(1000, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update historical rates for {BaseCurrency}/{TargetCurrency}",
                    baseCurrency, targetCurrency);
            }
        }

        _logger.LogInformation("Historical rates update completed. Added {UpdatedRates} rates from {TotalAttempts} currency pairs",
            updatedRates, totalAttempts);
    }

    public async Task<bool> ShouldUpdateRatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if we have recent rates (within last hour)
            var recentRates = await _exchangeRateRepository.GetLatestRatesAsync("USD", cancellationToken);
            var latestRate = recentRates.OrderByDescending(r => r.CreatedAt).FirstOrDefault();

            if (latestRate == null)
            {
                _logger.LogDebug("No existing rates found, update needed");
                return true;
            }

            var timeSinceLastUpdate = DateTime.UtcNow - latestRate.CreatedAt;
            var shouldUpdate = timeSinceLastUpdate > TimeSpan.FromMinutes(30);

            _logger.LogDebug("Last update was {TimeSinceUpdate} ago, should update: {ShouldUpdate}",
                timeSinceLastUpdate, shouldUpdate);

            return shouldUpdate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if rates should be updated");
            return true; // Default to updating on error
        }
    }

    private List<(string baseCurrency, string targetCurrency)> GenerateCurrencyPairs()
    {
        var pairs = new List<(string, string)>();

        // Generate major currency pairs
        for (int i = 0; i < _majorCurrencies.Length; i++)
        {
            for (int j = 0; j < _majorCurrencies.Length; j++)
            {
                if (i != j)
                {
                    pairs.Add((_majorCurrencies[i], _majorCurrencies[j]));
                }
            }
        }

        return pairs.Take(20).ToList(); // Limit to prevent excessive API calls
    }

    private async Task ClearRateCache(string baseCurrency, CancellationToken cancellationToken)
    {
        try
        {
            // Clear cache for all rates involving this base currency
            foreach (var targetCurrency in _majorCurrencies)
            {
                if (targetCurrency != baseCurrency)
                {
                    var cacheKey = $"rate_{baseCurrency}_{targetCurrency}_current";
                    await _cacheService.RemoveAsync(cacheKey, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear cache for base currency {BaseCurrency}", baseCurrency);
        }
    }
}

