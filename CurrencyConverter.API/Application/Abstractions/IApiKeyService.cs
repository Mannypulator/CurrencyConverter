using System;
using CurrencyConverter.API.Domain.Entity;

namespace CurrencyConverter.API.Application.Abstractions;

public interface IApiKeyService
{
    Task<ApiKey?> GetApiKeyAsync(string keyValue, CancellationToken cancellationToken = default);
    Task<bool> IsValidApiKeyAsync(string keyValue, CancellationToken cancellationToken = default);
    Task<bool> CheckRateLimitAsync(string keyValue, string ipAddress, CancellationToken cancellationToken = default);
    Task RecordUsageAsync(string keyValue, string ipAddress, string endpoint, int statusCode, CancellationToken cancellationToken = default);
}
