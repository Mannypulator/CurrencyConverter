using System;
using CurrencyConverter.API.Application.Abstractions;
using CurrencyConverter.API.Domain.DTOs;

namespace CurrencyConverter.API.Infrastructure.Services;

public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly IExchangeRateRepository _exchangeRateRepository;
    private readonly ICurrencyRepository _currencyRepository;
    private readonly IExternalCurrencyService _externalCurrencyService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CurrencyConversionService> _logger;

    public CurrencyConversionService(
        IExchangeRateRepository exchangeRateRepository,
        ICurrencyRepository currencyRepository,
        IExternalCurrencyService externalCurrencyService,
        ICacheService cacheService,
        ILogger<CurrencyConversionService> logger)
    {
        _exchangeRateRepository = exchangeRateRepository;
        _currencyRepository = currencyRepository;
        _externalCurrencyService = externalCurrencyService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ConversionResponseDto?> ConvertCurrencyAsync(ConversionRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Converting {Amount} {FromCurrency} to {ToCurrency} for date {Date}",
                request.Amount, request.FromCurrency, request.ToCurrency, request.Date?.ToShortDateString() ?? "current");

            // Validate currencies exist
            if (!await _currencyRepository.CurrencyExistsAsync(request.FromCurrency, cancellationToken))
            {
                _logger.LogWarning("Source currency {FromCurrency} not found", request.FromCurrency);
                return null;
            }

            if (!await _currencyRepository.CurrencyExistsAsync(request.ToCurrency, cancellationToken))
            {
                _logger.LogWarning("Target currency {ToCurrency} not found", request.ToCurrency);
                return null;
            }

            // Handle same currency conversion
            if (request.FromCurrency.Equals(request.ToCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return new ConversionResponseDto
                {
                    FromCurrency = request.FromCurrency.ToUpper(),
                    ToCurrency = request.ToCurrency.ToUpper(),
                    OriginalAmount = request.Amount,
                    ConvertedAmount = request.Amount,
                    ExchangeRate = 1.0m,
                    RateDate = request.Date ?? DateTime.UtcNow,
                    ConversionTime = DateTime.UtcNow
                };
            }

            // Get exchange rate
            var exchangeRate = await GetExchangeRateAsync(request.FromCurrency, request.ToCurrency, request.Date, cancellationToken);

            if (!exchangeRate.HasValue)
            {
                _logger.LogWarning("No exchange rate found for {FromCurrency}/{ToCurrency}", request.FromCurrency, request.ToCurrency);
                return null;
            }

            var convertedAmount = request.Amount * exchangeRate.Value;

            var response = new ConversionResponseDto
            {
                FromCurrency = request.FromCurrency.ToUpper(),
                ToCurrency = request.ToCurrency.ToUpper(),
                OriginalAmount = request.Amount,
                ConvertedAmount = Math.Round(convertedAmount, 2),
                ExchangeRate = exchangeRate.Value,
                RateDate = request.Date ?? DateTime.UtcNow,
                ConversionTime = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully converted {Amount} {FromCurrency} to {ConvertedAmount} {ToCurrency} at rate {Rate}",
                request.Amount, request.FromCurrency, response.ConvertedAmount, request.ToCurrency, exchangeRate.Value);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting currency from {FromCurrency} to {ToCurrency}", request.FromCurrency, request.ToCurrency);
            throw;
        }
    }

    public async Task<HistoricalRatesResponseDto?> GetHistoricalRatesAsync(HistoricalRatesRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving historical rates for {BaseCurrency}/{TargetCurrency} from {StartDate} to {EndDate}",
                request.BaseCurrency, request.TargetCurrency, request.StartDate.ToShortDateString(), request.EndDate.ToShortDateString());

            // Validate date range
            if (request.StartDate > request.EndDate)
            {
                _logger.LogWarning("Invalid date range: StartDate {StartDate} is after EndDate {EndDate}",
                    request.StartDate, request.EndDate);
                return null;
            }

            // Validate currencies
            if (!await _currencyRepository.CurrencyExistsAsync(request.BaseCurrency, cancellationToken) ||
                !await _currencyRepository.CurrencyExistsAsync(request.TargetCurrency, cancellationToken))
            {
                _logger.LogWarning("Invalid currency pair: {BaseCurrency}/{TargetCurrency}", request.BaseCurrency, request.TargetCurrency);
                return null;
            }

            // Check cache first
            var cacheKey = $"historical_rates_{request.BaseCurrency}_{request.TargetCurrency}_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}";
            var cachedRates = await _cacheService.GetAsync<HistoricalRatesResponseDto>(cacheKey, cancellationToken);

            if (cachedRates != null)
            {
                _logger.LogDebug("Retrieved historical rates from cache");
                return cachedRates;
            }

            // Get from database
            var exchangeRates = await _exchangeRateRepository.GetHistoricalRatesAsync(
                request.BaseCurrency, request.TargetCurrency, request.StartDate, request.EndDate, cancellationToken);

            var response = new HistoricalRatesResponseDto
            {
                BaseCurrency = request.BaseCurrency.ToUpper(),
                TargetCurrency = request.TargetCurrency.ToUpper(),
                Rates = exchangeRates.ToDictionary(r => r.Date.Date, r => r.Rate)
            };

            // If we have rates, cache them
            if (response.Rates.Any())
            {
                await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(1), cancellationToken);
            }

            _logger.LogInformation("Retrieved {Count} historical rates for {BaseCurrency}/{TargetCurrency}",
                response.Rates.Count, request.BaseCurrency, request.TargetCurrency);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical rates for {BaseCurrency}/{TargetCurrency}",
                request.BaseCurrency, request.TargetCurrency);
            throw;
        }
    }

    public async Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first for real-time rates
            if (!date.HasValue)
            {
                var cacheKey = $"rate_{fromCurrency}_{toCurrency}_current";
                var cachedRate = await _cacheService.GetAsync<decimal?>(cacheKey, cancellationToken);

                if (cachedRate.HasValue)
                {
                    _logger.LogDebug("Retrieved exchange rate from cache: {FromCurrency}/{ToCurrency} = {Rate}",
                        fromCurrency, toCurrency, cachedRate.Value);
                    return cachedRate.Value;
                }
            }

            // Try to get direct rate
            var exchangeRate = await _exchangeRateRepository.GetRateAsync(fromCurrency, toCurrency, date, cancellationToken);

            if (exchangeRate != null)
            {
                if (!date.HasValue)
                {
                    var cacheKey = $"rate_{fromCurrency}_{toCurrency}_current";
                    await _cacheService.SetAsync(cacheKey, exchangeRate.Rate, TimeSpan.FromMinutes(5), cancellationToken);
                }

                return exchangeRate.Rate;
            }

            // Try inverse rate
            var inverseRate = await _exchangeRateRepository.GetRateAsync(toCurrency, fromCurrency, date, cancellationToken);

            if (inverseRate != null && inverseRate.Rate != 0)
            {
                var rate = 1 / inverseRate.Rate;

                // Cache real-time rates
                if (!date.HasValue)
                {
                    var cacheKey = $"rate_{fromCurrency}_{toCurrency}_current";
                    await _cacheService.SetAsync(cacheKey, rate, TimeSpan.FromMinutes(5), cancellationToken);
                }

                return rate;
            }

            // For real-time rates, try to fetch from external service
            if (!date.HasValue)
            {
                try
                {
                    var externalRates = await _externalCurrencyService.GetRealTimeRatesAsync(fromCurrency, cancellationToken);

                    if (externalRates?.Rates != null && externalRates.Rates.TryGetValue(toCurrency, out var externalRate))
                    {
                        // Cache the fetched rate
                        var cacheKey = $"rate_{fromCurrency}_{toCurrency}_current";
                        await _cacheService.SetAsync(cacheKey, externalRate, TimeSpan.FromMinutes(5), cancellationToken);

                        return externalRate;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch real-time rate from external service for {FromCurrency}/{ToCurrency}",
                        fromCurrency, toCurrency);
                }
            }

            _logger.LogWarning("No exchange rate found for {FromCurrency}/{ToCurrency} on {Date}",
                fromCurrency, toCurrency, date?.ToShortDateString() ?? "current");

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rate for {FromCurrency}/{ToCurrency}", fromCurrency, toCurrency);
            throw;
        }
    }
}
