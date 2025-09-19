using System;
using CurrencyConverter.API.Domain.Entity;

namespace CurrencyConverter.API.Application.Abstractions;

public interface IExchangeRateRepository
{
    Task<ExchangeRate?> GetRateAsync(string baseCurrency, string targetCurrency, DateTime? date = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, string targetCurrency, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<ExchangeRate> AddRateAsync(ExchangeRate exchangeRate, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExchangeRate>> AddRatesAsync(IEnumerable<ExchangeRate> exchangeRates, CancellationToken cancellationToken = default);
    Task<bool> UpdateRateAsync(ExchangeRate exchangeRate, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string baseCurrency, CancellationToken cancellationToken = default);
}