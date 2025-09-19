using System;

namespace CurrencyConverter.API.Application.Abstractions;

public interface IRateUpdateService
{
    Task UpdateRealTimeRatesAsync(CancellationToken cancellationToken = default);
    Task UpdateHistoricalRatesAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<bool> ShouldUpdateRatesAsync(CancellationToken cancellationToken = default);
}
