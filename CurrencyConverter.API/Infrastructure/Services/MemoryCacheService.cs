using System;
using System.Text.Json;
using CurrencyConverter.API.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.API.Infrastructure.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly MemoryCacheEntryOptions _defaultOptions;

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;

        _defaultOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Priority = CacheItemPriority.Normal
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out var cachedValue))
            {
                if (cachedValue is T directValue)
                {
                    _logger.LogDebug("Cache hit for key: {Key} (direct type)", key);
                    return directValue;
                }

                if (cachedValue is string jsonValue)
                {
                    var deserializedValue = JsonSerializer.Deserialize<T>(jsonValue, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogDebug("Cache hit for key: {Key} (deserialized from JSON)", key);
                    return deserializedValue;
                }
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached value for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (value == null)
            {
                _logger.LogWarning("Attempted to cache null value for key: {Key}", key);
                return;
            }

            var options = _defaultOptions;

            if (expiration.HasValue)
            {
                options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration.Value,
                    Priority = CacheItemPriority.Normal
                };
            }

            // For complex types, serialize to JSON to ensure proper storage
            if (typeof(T) != typeof(string) && !typeof(T).IsPrimitive)
            {
                var jsonValue = JsonSerializer.Serialize(value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _memoryCache.Set(key, jsonValue, options);
            }
            else
            {
                _memoryCache.Set(key, value, options);
            }

            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}",
                key, expiration?.ToString() ?? _defaultOptions.AbsoluteExpirationRelativeToNow?.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Removed cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = _memoryCache.TryGetValue(key, out _);
            _logger.LogDebug("Cache existence check for key: {Key} - {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }
}
