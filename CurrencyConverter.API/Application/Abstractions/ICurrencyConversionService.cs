using System;
using CurrencyConverter.API.Domain.DTOs;

namespace CurrencyConverter.API.Application.Abstractions;

public interface ICurrencyConversionService
{
    Task<ConversionResponseDto?> ConvertCurrencyAsync(ConversionRequestDto request, CancellationToken cancellationToken = default);
    Task<HistoricalRatesResponseDto?> GetHistoricalRatesAsync(HistoricalRatesRequestDto request, CancellationToken cancellationToken = default);
    Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime? date = null, CancellationToken cancellationToken = default);
}
