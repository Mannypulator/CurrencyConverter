using System;
using System.Text.Json;
using CurrencyConverter.API.Application.Abstractions;
using CurrencyConverter.API.Domain.DTOs;

namespace CurrencyConverter.API.Infrastructure.Services;

public class ExternalCurrencyService : IExternalCurrencyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalCurrencyService> _logger;
    private readonly IConfiguration _configuration;
    private readonly bool _useSimulatedData;

    public ExternalCurrencyService(
        HttpClient httpClient,
        ILogger<ExternalCurrencyService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _useSimulatedData = _configuration.GetValue<bool>("ExternalService:UseSimulatedData", true);
    }

    public async Task<ExternalRealTimeRatesDto?> GetRealTimeRatesAsync(string baseCurrency, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching real-time rates for base currency: {BaseCurrency}", baseCurrency);

            if (_useSimulatedData)
            {
                return GetSimulatedRealTimeRates(baseCurrency);
            }

            var endpoint = _configuration["ExternalService:RealTimeEndpoint"];
            var response = await _httpClient.GetAsync($"{endpoint}?base={baseCurrency}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("External service returned status code: {StatusCode} for real-time rates", response.StatusCode);
                return HandleErrorResponse(response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var rates = JsonSerializer.Deserialize<ExternalRealTimeRatesDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Successfully fetched real-time rates for {BaseCurrency}", baseCurrency);
            return rates;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching real-time rates for {BaseCurrency}", baseCurrency);
            throw new InvalidOperationException($"Failed to fetch real-time rates: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request timeout while fetching real-time rates for {BaseCurrency}", baseCurrency);
            throw new InvalidOperationException("Request timeout while fetching exchange rates", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize real-time rates response for {BaseCurrency}", baseCurrency);
            throw new InvalidOperationException("Invalid response format from external service", ex);
        }
    }

    public async Task<ExternalHistoricalRatesDto?> GetHistoricalRatesAsync(string baseCurrency, string targetCurrency, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching historical rates for {BaseCurrency} to {TargetCurrency} from {StartDate} to {EndDate}",
                baseCurrency, targetCurrency, startDate.ToShortDateString(), endDate.ToShortDateString());

            if (_useSimulatedData)
            {
                return GetSimulatedHistoricalRates(baseCurrency, targetCurrency, startDate, endDate);
            }

            var endpoint = _configuration["ExternalService:HistoricalEndpoint"];
            var response = await _httpClient.GetAsync(
                $"{endpoint}?base={baseCurrency}&target={targetCurrency}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("External service returned status code: {StatusCode} for historical rates", response.StatusCode);
                return HandleHistoricalErrorResponse(response.StatusCode);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var rates = JsonSerializer.Deserialize<ExternalHistoricalRatesDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Successfully fetched historical rates for {BaseCurrency} to {TargetCurrency}", baseCurrency, targetCurrency);
            return rates;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching historical rates");
            throw new InvalidOperationException($"Failed to fetch historical rates: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request timeout while fetching historical rates");
            throw new InvalidOperationException("Request timeout while fetching historical rates", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize historical rates response");
            throw new InvalidOperationException("Invalid response format from external service", ex);
        }
    }

    private ExternalRealTimeRatesDto GetSimulatedRealTimeRates(string baseCurrency)
    {
        var random = new Random();
        var baseRates = GetBaseRates();

        var rates = new Dictionary<string, decimal>();
        var baseRate = baseRates.GetValueOrDefault(baseCurrency, 1m);

        foreach (var currency in baseRates.Keys.Where(c => c != baseCurrency))
        {
            var targetRate = baseRates[currency];
            var rate = targetRate / baseRate;

            // Add some random fluctuation (±2%)
            var fluctuation = (decimal)(random.NextDouble() * 0.04 - 0.02);
            rate *= (1 + fluctuation);

            rates[currency] = Math.Round(rate, 8);
        }

        return new ExternalRealTimeRatesDto
        {
            Base = baseCurrency,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Rates = rates
        };
    }

    private ExternalHistoricalRatesDto GetSimulatedHistoricalRates(string baseCurrency, string targetCurrency, DateTime startDate, DateTime endDate)
    {
        var random = new Random();
        var rates = new Dictionary<string, decimal>();
        var baseRates = GetBaseRates();

        var baseRate = baseRates.GetValueOrDefault(baseCurrency, 1m);
        var targetRate = baseRates.GetValueOrDefault(targetCurrency, 1m);
        var currentRate = targetRate / baseRate;

        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            // Add some historical variation (±1% daily change)
            var dailyChange = (decimal)(random.NextDouble() * 0.02 - 0.01);
            currentRate *= (1 + dailyChange);

            rates[currentDate.ToString("yyyy-MM-dd")] = Math.Round(currentRate, 8);
            currentDate = currentDate.AddDays(1);
        }

        return new ExternalHistoricalRatesDto
        {
            Base = baseCurrency,
            Target = targetCurrency,
            Rates = rates
        };
    }

    private Dictionary<string, decimal> GetBaseRates()
    {
        // Simulated rates relative to USD
        return new Dictionary<string, decimal>
        {
            ["USD"] = 1.00m,
            ["EUR"] = 0.92m,
            ["GBP"] = 0.80m,
            ["JPY"] = 155.00m,
            ["CAD"] = 1.35m,
            ["AUD"] = 1.52m,
            ["CHF"] = 0.90m,
            ["CNY"] = 7.20m
        };
    }

    private ExternalRealTimeRatesDto? HandleErrorResponse(System.Net.HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.TooManyRequests => throw new InvalidOperationException("Rate limit exceeded from external service"),
            System.Net.HttpStatusCode.Unauthorized => throw new InvalidOperationException("Invalid API key for external service"),
            System.Net.HttpStatusCode.NotFound => throw new InvalidOperationException("Currency not found in external service"),
            System.Net.HttpStatusCode.InternalServerError => throw new InvalidOperationException("External service is currently unavailable"),
            _ => throw new InvalidOperationException($"External service error: {statusCode}")
        };
    }

    private ExternalHistoricalRatesDto? HandleHistoricalErrorResponse(System.Net.HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.TooManyRequests => throw new InvalidOperationException("Rate limit exceeded from external service"),
            System.Net.HttpStatusCode.Unauthorized => throw new InvalidOperationException("Invalid API key for external service"),
            System.Net.HttpStatusCode.BadRequest => throw new InvalidOperationException("Invalid date range or currency pair"),
            System.Net.HttpStatusCode.NotFound => throw new InvalidOperationException("Historical data not available for the specified period"),
            System.Net.HttpStatusCode.InternalServerError => throw new InvalidOperationException("External service is currently unavailable"),
            _ => throw new InvalidOperationException($"External service error: {statusCode}")
        };
    }
}
