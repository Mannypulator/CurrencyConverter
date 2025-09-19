using System;
using System.Net;
using System.Text.Json;
using CurrencyConverter.API.Application.Abstractions;
using Microsoft.Extensions.Primitives;

namespace CurrencyConverter.API.Middlewares;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        // Skip rate limiting for health checks and swagger
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        // Extract API key from header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out StringValues apiKeyValues))
        {
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "API key is required");
            return;
        }

        var apiKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "API key is required");
            return;
        }

        // Validate API key
        if (!await apiKeyService.IsValidApiKeyAsync(apiKey))
        {
            _logger.LogWarning("Invalid API key attempted: {ApiKey}", apiKey);
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "Invalid API key");
            return;
        }

        // Check rate limit
        var ipAddress = GetClientIpAddress(context);
        if (!await apiKeyService.CheckRateLimitAsync(apiKey, ipAddress))
        {
            _logger.LogWarning("Rate limit exceeded for API key: {ApiKey}, IP: {IpAddress}", apiKey, ipAddress);
            await WriteErrorResponse(context, HttpStatusCode.TooManyRequests, "Rate limit exceeded");
            return;
        }

        // Continue to next middleware
        await _next(context);

        // Record usage after request completion
        try
        {
            await apiKeyService.RecordUsageAsync(apiKey, ipAddress, context.Request.Path, context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record API usage");
            // Don't fail the request for logging issues
        }
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP addresses first
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var firstIp = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(firstIp))
                return firstIp;
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            var ip = realIp.FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = message,
            statusCode = (int)statusCode,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
