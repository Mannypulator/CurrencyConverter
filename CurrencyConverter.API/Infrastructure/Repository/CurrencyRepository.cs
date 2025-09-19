using System;
using CurrencyConverter.API.Application.Abstractions;
using CurrencyConverter.API.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace CurrencyConverter.API.Infrastructure.Repository;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly CurrencyDbContext _context;
    private readonly ILogger<CurrencyRepository> _logger;

    public CurrencyRepository(CurrencyDbContext context, ILogger<CurrencyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Currency?> GetCurrencyAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var currency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.Code == code.ToUpper(), cancellationToken);

            _logger.LogDebug("Retrieved currency: {CurrencyCode} - {Found}", code, currency != null ? "Found" : "Not Found");

            return currency;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving currency {Code}", code);
            throw;
        }
    }

    public async Task<IEnumerable<Currency>> GetAllCurrenciesAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Currencies.AsQueryable();

            if (activeOnly)
            {
                query = query.Where(c => c.IsActive);
            }

            var currencies = await query.OrderBy(c => c.Code).ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} currencies (activeOnly: {ActiveOnly})", currencies.Count, activeOnly);

            return currencies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving currencies");
            throw;
        }
    }

    public async Task<bool> CurrencyExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _context.Currencies
                .AnyAsync(c => c.Code == code.ToUpper() && c.IsActive, cancellationToken);

            _logger.LogDebug("Currency {Code} exists: {Exists}", code, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if currency {Code} exists", code);
            throw;
        }
    }
}
