using System;
using CurrencyConverter.API.Application.Abstractions;
using CurrencyConverter.API.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace CurrencyConverter.API.Infrastructure.Repository;

public class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly CurrencyDbContext _context;
    private readonly ILogger<ExchangeRateRepository> _logger;

    public ExchangeRateRepository(CurrencyDbContext context, ILogger<ExchangeRateRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExchangeRate?> GetRateAsync(string baseCurrency, string targetCurrency, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.ExchangeRates
                .Where(r => r.BaseCurrencyCode == baseCurrency && r.TargetCurrencyCode == targetCurrency);

            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                query = query.Where(r => r.Date.Date == targetDate);
            }
            else
            {
                // Get the most recent rate
                query = query.Where(r => !r.IsHistorical)
                          .OrderByDescending(r => r.Date);
            }

            var rate = await query.FirstOrDefaultAsync(cancellationToken);

            _logger.LogDebug("Retrieved exchange rate for {BaseCurrency}/{TargetCurrency} on {Date}: {Rate}",
                baseCurrency, targetCurrency, date?.ToShortDateString() ?? "latest", rate?.Rate);

            return rate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exchange rate for {BaseCurrency}/{TargetCurrency}", baseCurrency, targetCurrency);
            throw;
        }
    }

    public async Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, string targetCurrency, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var rates = await _context.ExchangeRates
                .Where(r => r.BaseCurrencyCode == baseCurrency &&
                          r.TargetCurrencyCode == targetCurrency &&
                          r.Date.Date >= startDate.Date &&
                          r.Date.Date <= endDate.Date)
                .OrderBy(r => r.Date)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} historical rates for {BaseCurrency}/{TargetCurrency} from {StartDate} to {EndDate}",
                rates.Count, baseCurrency, targetCurrency, startDate.ToShortDateString(), endDate.ToShortDateString());

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical rates for {BaseCurrency}/{TargetCurrency}", baseCurrency, targetCurrency);
            throw;
        }
    }

    public async Task<ExchangeRate> AddRateAsync(ExchangeRate exchangeRate, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.ExchangeRates.Add(exchangeRate);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Added exchange rate for {BaseCurrency}/{TargetCurrency}: {Rate} on {Date}",
                exchangeRate.BaseCurrencyCode, exchangeRate.TargetCurrencyCode, exchangeRate.Rate, exchangeRate.Date);

            return exchangeRate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding exchange rate for {BaseCurrency}/{TargetCurrency}",
                exchangeRate.BaseCurrencyCode, exchangeRate.TargetCurrencyCode);
            throw;
        }
    }

    public async Task<IEnumerable<ExchangeRate>> AddRatesAsync(IEnumerable<ExchangeRate> exchangeRates, CancellationToken cancellationToken = default)
    {
        try
        {
            var ratesList = exchangeRates.ToList();
            _context.ExchangeRates.AddRange(ratesList);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Added {Count} exchange rates", ratesList.Count);

            return ratesList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple exchange rates");
            throw;
        }
    }

    public async Task<bool> UpdateRateAsync(ExchangeRate exchangeRate, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingRate = await _context.ExchangeRates
                .FirstOrDefaultAsync(r => r.Id == exchangeRate.Id, cancellationToken);

            if (existingRate == null)
            {
                return false;
            }

            existingRate.Rate = exchangeRate.Rate;
            existingRate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Updated exchange rate for {BaseCurrency}/{TargetCurrency}: {Rate}",
                existingRate.BaseCurrencyCode, existingRate.TargetCurrencyCode, existingRate.Rate);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating exchange rate with ID {Id}", exchangeRate.Id);
            throw;
        }
    }

    public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string baseCurrency, CancellationToken cancellationToken = default)
    {
        try
        {
            var rates = await _context.ExchangeRates
                .Where(r => r.BaseCurrencyCode == baseCurrency && !r.IsHistorical)
                .GroupBy(r => r.TargetCurrencyCode)
                .Select(g => g.OrderByDescending(r => r.Date).First())
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} latest rates for base currency {BaseCurrency}", rates.Count, baseCurrency);

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest rates for base currency {BaseCurrency}", baseCurrency);
            throw;
        }
    }
}

