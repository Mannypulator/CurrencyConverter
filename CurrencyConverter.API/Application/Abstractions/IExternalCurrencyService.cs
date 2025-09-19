using System;
using CurrencyConverter.API.Domain.DTOs;

namespace CurrencyConverter.API.Application.Abstractions;

public interface IExternalCurrencyService
{
    Task<ExternalRealTimeRatesDto?> GetRealTimeRatesAsync(string baseCurrency, CancellationToken cancellationToken = default);
    Task<ExternalHistoricalRatesDto?> GetHistoricalRatesAsync(string baseCurrency, string targetCurrency, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
