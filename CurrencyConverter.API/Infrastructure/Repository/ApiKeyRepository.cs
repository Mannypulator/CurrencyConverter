using System;
using CurrencyConverter.API.Application.Abstractions;
using CurrencyConverter.API.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace CurrencyConverter.API.Infrastructure.Repository;

public class ApiKeyRepository : IApiKeyService
{
    private readonly CurrencyDbContext _context;
    private readonly ILogger<ApiKeyRepository> _logger;

    public ApiKeyRepository(CurrencyDbContext context, ILogger<ApiKeyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiKey?> GetApiKeyAsync(string keyValue, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.KeyValue == keyValue && k.IsActive, cancellationToken);

            return apiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key");
            throw;
        }
    }

    public async Task<bool> IsValidApiKeyAsync(string keyValue, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await GetApiKeyAsync(keyValue, cancellationToken);

            if (apiKey == null)
                return false;

            if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return false;
        }
    }

    public async Task<bool> CheckRateLimitAsync(string keyValue, string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await GetApiKeyAsync(keyValue, cancellationToken);
            if (apiKey == null)
                return false;

            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentRequests = await _context.ApiKeyUsages
                .CountAsync(u => u.ApiKeyId == apiKey.Id && u.RequestTime > oneHourAgo, cancellationToken);

            var withinLimit = recentRequests < apiKey.RequestsPerHour;

            _logger.LogDebug("Rate limit check for API key {KeyId}: {RecentRequests}/{Limit} - {Status}",
                apiKey.Id, recentRequests, apiKey.RequestsPerHour, withinLimit ? "OK" : "EXCEEDED");

            return withinLimit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for API key");
            return false;
        }
    }

    public async Task RecordUsageAsync(string keyValue, string ipAddress, string endpoint, int statusCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await GetApiKeyAsync(keyValue, cancellationToken);
            if (apiKey == null)
                return;

            var usage = new ApiKeyUsage
            {
                ApiKeyId = apiKey.Id,
                RequestTime = DateTime.UtcNow,
                IpAddress = ipAddress,
                Endpoint = endpoint,
                ResponseStatusCode = statusCode
            };

            _context.ApiKeyUsages.Add(usage);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Recorded API usage for key {KeyId}: {Endpoint} - {StatusCode}",
                apiKey.Id, endpoint, statusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording API usage");
            // Don't throw here as this shouldn't break the main flow
        }
    }
}
