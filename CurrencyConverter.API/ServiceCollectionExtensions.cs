using System;
using CurrencyConverter.API.Application.Abstractions;
using CurrencyConverter.API.Infrastructure.Repository;
using CurrencyConverter.API.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Polly;
using Polly.Extensions.Http;

namespace CurrencyConverter.API;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IApiKeyService, ApiKeyRepository>();

        // Register services
        services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
        services.AddScoped<IRateUpdateService, RateUpdateService>();
        services.AddScoped<ICacheService, MemoryCacheService>();

        // Configure API versioning
        services.AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-API-Version")
            );
        });

        services.AddVersionedApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    public static IHttpClientBuilder AddRetryPolicy(this IHttpClientBuilder builder)
    {
        return builder.AddPolicyHandler(GetRetryPolicy());
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    if (outcome.Exception != null)
                    {
                        logger?.LogWarning("Retry {RetryCount} after {Delay}ms due to: {Exception}",
                            retryCount, timespan.TotalMilliseconds, outcome.Exception.Message);
                    }
                    else
                    {
                        logger?.LogWarning("Retry {RetryCount} after {Delay}ms due to status code: {StatusCode}",
                            retryCount, timespan.TotalMilliseconds, outcome.Result.StatusCode);
                    }
                });
    }

    private static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue("logger", out var logger) && logger is ILogger log)
        {
            return log;
        }
        return null;
    }
}

/// <summary>
/// Custom API versioning attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiVersionAttribute : Attribute
{
    public string Version { get; }

    public ApiVersionAttribute(string version)
    {
        Version = version;
    }
}