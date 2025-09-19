using System;

namespace CurrencyConverter.API.Middlewares;

public class ApiVersionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiVersionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
     
        var version = GetApiVersion(context);

        context.Items["ApiVersion"] = version;

        await _next(context);
    }

    private string GetApiVersion(HttpContext context)
    {

        var path = context.Request.Path.Value;
        if (!string.IsNullOrEmpty(path))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && segments[1].StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                return segments[1].ToLower();
            }
        }

      
        if (context.Request.Headers.TryGetValue("X-API-Version", out var headerVersion))
        {
            var version = headerVersion.FirstOrDefault();
            if (!string.IsNullOrEmpty(version))
            {
                return version.ToLower();
            }
        }

       
        return "v1";
    }
}
