
using CurrencyConverter.API;
using CurrencyConverter.API.Application.Abstractions;
using CurrencyConverter.API.BackgroundServices;
using CurrencyConverter.API.Infrastructure.Repository;
using CurrencyConverter.API.Infrastructure.Services;
using CurrencyConverter.API.Middlewares;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Currency Converter API",
        Version = "v1",
        Description = "A comprehensive currency conversion API with real-time and historical exchange rates",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@currencyconverter.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License"
        }
    });

    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Currency Converter API",
        Version = "v2",
        Description = "Enhanced currency conversion API with batch operations and additional metadata",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@currencyconverter.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License"
        }
    });

    // Add API Key authentication
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key needed to access the endpoints. Get your API key from the admin panel."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    try
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    }
    catch (Exception ex)
    {
        Log.Warning("Could not load XML documentation: {Message}", ex.Message);
    }

});

// Add database context
builder.Services.AddDbContext<CurrencyDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient with retry policies
builder.Services.AddHttpClient<IExternalCurrencyService, ExternalCurrencyService>()
    .AddRetryPolicy();

// Register application services
builder.Services.RegisterApplicationServices();

// Add background services
builder.Services.AddHostedService<CurrencyRateUpdaterService>();

// Add memory cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency Converter API V1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Currency Converter API V2");
        c.DefaultModelsExpandDepth(-1); // Hide schemas section
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // Collapse operations by default
        c.DisplayRequestDuration(); // Show request duration

        // Custom CSS for better appearance
        c.InjectStylesheet("/swagger-ui/custom.css");

        // Enable deep linking
        c.EnableDeepLinking();

        // Enable filter
        c.EnableFilter();

        // Show common parameters
        // c.ShowCommonParameters();

        // Custom header for API key instructions
        c.HeadContent = @"
            <style>
                .swagger-ui .info { margin: 20px 0; }
                .api-key-info { 
                    background: #f8f9fa; 
                    border: 1px solid #dee2e6; 
                    border-radius: 4px; 
                    padding: 15px; 
                    margin: 20px 0; 
                }
                .api-key-info h4 { margin-top: 0; color: #495057; }
                .api-key-info code { 
                    background: #e9ecef; 
                    padding: 2px 4px; 
                    border-radius: 3px; 
                    font-size: 87.5%; 
                }
            </style>";
    });
}

app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Serve custom CSS for Swagger
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add API key information endpoint (for development)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/keys", () => new
    {
        message = "Available API Keys for Testing",
        keys = new[]
        {
            new { name = "Demo Key", key = "demo-key-123456789", limit = "1,000 requests/hour" },
            new { name = "Premium Key", key = "premium-key-987654321", limit = "5,000 requests/hour" }
        },
        usage = "Add the API key to the 'X-API-Key' header in your requests"
    }).WithTags("Development").WithSummary("Get available API keys for testing");
}

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CurrencyDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();