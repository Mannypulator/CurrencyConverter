# Currency Converter API

A comprehensive ASP.NET Core Web API for currency conversion with real-time and historical exchange rates, built with enterprise-grade features including rate limiting, caching, background services, and robust error handling.

## 🚀 Features

### Core Functionality

- **Real-time Currency Conversion**: Convert amounts between currencies using current exchange rates
- **Historical Currency Conversion**: Convert amounts using historical exchange rates for specific dates
- **Historical Rate Retrieval**: Get exchange rates for date ranges
- **Multi-Currency Support**: Support for major world currencies (USD, EUR, GBP, JPY, CAD, AUD, CHF, CNY)

### Advanced Features

- **API Versioning**: Support for V1 and V2 API versions with different feature sets
- **Rate Limiting**: API key-based rate limiting with configurable limits per hour
- **Caching**: Memory caching for improved performance with configurable expiration
- **Background Services**: Automatic updating of exchange rates from external sources
- **Retry Logic**: Exponential backoff retry mechanism for external service calls
- **Comprehensive Logging**: Structured logging with Serilog
- **Error Handling**: Global exception handling with appropriate HTTP status codes
- **Input Validation**: Comprehensive request validation with detailed error messages

### Technical Architecture

- **Clean Architecture**: Separated concerns with Domain, Application, Infrastructure, and API layers
- **Entity Framework Core**: Database operations with SQLLite
- **Dependency Injection**: Full DI container integration
- **Async/Await**: Fully asynchronous operations for better performance
- **SOLID Principles**: Adherence to software engineering best practices

## 🏗️ Architecture

### Project Structure

```
CurrencyConverter.sln
CurrencyConverter.API/
├── appsettings.Development.json
├── appsettings.json
├── CurrencyConverter.API.csproj
├── CurrencyConverter.API.http
├── CurrencyConverter.API.xml
├── CurrencyConverter.db
├── Program.cs
├── ReadMe.md
├── ServiceCollectionExtensions.cs
├── WeatherForecast.cs
├── Application/
│   ├── Abstractions/
│   │   ├── IApiKeyService.cs
│   │   ├── ICacheService.cs
│   │   ├── ICurrencyConversionService.cs
│   │   ├── ICurrencyRepository.cs
│   │   ├── IExchangeRateRepository.cs
│   │   ├── IExternalCurrencyService.cs
│   │   └── IRateUpdateService.cs
│   └── BackgroundServices/
│       └── CurrencyRateUpdaterService.cs
├── Controllers/
│   ├── v1/
│   │   ├── ConvertController.cs
│   │   ├── CurrenciesController.cs
│   │   └── RatesController.cs
│   └── v2/
│       └── ConvertController.cs
├── Domain/
│   ├── DTOs/
│   │   ├── ConversionRequestDto.cs
│   │   ├── ExternalRealTimeRatesDto.cs
│   │   └── HistoricalRatesRequestDto.cs
│   └── Entity/
│       ├── ApiKey.cs
│       ├── ApiKeyUsage.cs
│       ├── Currency.cs
│       └── ExchangeRate.cs
├── Infrastructure/
│   ├── Repository/
│   │   ├── ApiKeyRepository.cs
│   │   ├── CurrencyDbContext.cs
│   │   ├── CurrencyRepository.cs
│   │   └── ExchangeRateRepository.cs
│   └── Services/
│       ├── CurrencyConversionService.cs
│       ├── ExternalCurrencyService.cs
│       ├── MemoryCacheService.cs
│       └── RateUpdateService.cs
├── logs/
│   └── currency-converter-YYYYMMDD.txt
├── Middlewares/
│   ├── ApiVersionMiddleware.cs
│   ├── ExceptionHandlingMiddleware.cs
│   └── RateLimitingMiddleware.cs
├── Migrations/
│   ├── 20250919160711_initial.cs
│   ├── 20250919160711_initial.Designer.cs
│   └── CurrencyDbContextModelSnapshot.cs
├── Properties/
│   └── launchSettings.json
└── obj/ & bin/           # Build output and intermediate files
```

### Database Schema

#### Currencies

- **Code** (PK): 3-letter currency code (USD, EUR, etc.)
- **Name**: Full currency name
- **Symbol**: Currency symbol
- **IsActive**: Whether currency is currently supported
- **CreatedAt**: Creation timestamp

#### ExchangeRates

- **Id** (PK): Unique identifier
- **BaseCurrencyCode** (FK): Source currency
- **TargetCurrencyCode** (FK): Target currency
- **Rate**: Exchange rate (decimal 18,8)
- **Date**: Rate date
- **IsHistorical**: Whether this is historical or real-time data
- **CreatedAt/UpdatedAt**: Timestamps

#### ApiKeys

- **Id** (PK): Unique identifier
- **KeyValue**: API key string
- **Name**: Descriptive name
- **RequestsPerHour**: Rate limit
- **IsActive**: Whether key is active
- **ExpiresAt**: Optional expiration date

#### ApiKeyUsage

- **Id** (PK): Unique identifier
- **ApiKeyId** (FK): Reference to API key
- **RequestTime**: When request was made
- **IpAddress**: Client IP address
- **Endpoint**: API endpoint called
- **ResponseStatusCode**: HTTP response code

## 📊 API Endpoints

### V1 API Endpoints

#### Currency Conversion

```http
POST /api/v1/convert
Content-Type: application/json
X-API-Key: your-api-key

{
  "fromCurrency": "USD",
  "toCurrency": "EUR",
  "amount": 100.00
}
```

#### Historical Conversion

```http
POST /api/v1/convert/historical
Content-Type: application/json
X-API-Key: your-api-key

{
  "fromCurrency": "USD",
  "toCurrency": "EUR",
  "amount": 100.00,
  "date": "2024-01-15"
}
```

#### Get Exchange Rate

```http
GET /api/v1/convert/rate?from=USD&to=EUR
X-API-Key: your-api-key
```

#### Historical Rates

```http
GET /api/v1/rates/historical?baseCurrency=USD&targetCurrency=EUR&startDate=2024-01-01&endDate=2024-01-31
X-API-Key: your-api-key
```

#### Supported Currencies

```http
GET /api/v1/currencies
X-API-Key: your-api-key
```

### V2 API Endpoints (Enhanced)

#### Enhanced Conversion with Metadata

```http
POST /api/v2/convert
```

Returns additional metadata including API version, processing time, and disclaimers.

#### Batch Conversion

```http
POST /api/v2/convert/batch
Content-Type: application/json
X-API-Key: your-api-key

[
  {
    "fromCurrency": "USD",
    "toCurrency": "EUR",
    "amount": 100.00
  },
  {
    "fromCurrency": "GBP",
    "toCurrency": "JPY",
    "amount": 50.00
  }
]
```

## 🔧 Configuration

### Database Connection

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CurrencyConverterDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### External Service Configuration

```json
{
  "ExternalService": {
    "UseSimulatedData": true,
    "RealTimeEndpoint": "https://api.exchangerates.io/v1/latest",
    "HistoricalEndpoint": "https://api.exchangerates.io/v1/history",
    "RequestTimeout": "00:00:30",
    "MaxRetryAttempts": 3
  }
}
```

### Rate Limiting Configuration

```json
{
  "RateLimit": {
    "DefaultRequestsPerHour": 1000,
    "PremiumRequestsPerHour": 5000
  }
}
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server or SQL Server LocalDB
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**

```bash
git clone https://github.com/your-repo/currency-converter-api.git
cd currency-converter-api
```

2. **Restore NuGet packages**

```bash
dotnet restore
```

3. **Update database connection string** in `appsettings.json`

4. **Create and seed database**

```bash
dotnet ef database update
```

5. **Run the application**

```bash
dotnet run
```

6. **Access the API**
   - API: `https://localhost:5001`
   - Swagger UI: `https://localhost:5001/swagger`

### Sample API Keys

For testing purposes, the following API keys are pre-seeded:

- **Demo Key**: `demo-key-123456789` (1,000 requests/hour)
- **Premium Key**: `premium-key-987654321` (5,000 requests/hour)

## 🧪 Testing

### Using Swagger UI

1. Navigate to `/swagger`
2. Use the "Authorize" button to add your API key
3. Test endpoints directly from the UI

### Using Postman/curl

```bash
# Get current USD to EUR rate
curl -H "X-API-Key: demo-key-123456789" \
     "https://localhost:5001/api/v1/convert/rate?from=USD&to=EUR"

# Convert currency
curl -H "X-API-Key: demo-key-123456789" \
     -H "Content-Type: application/json" \
     -d '{"fromCurrency":"USD","toCurrency":"EUR","amount":100}' \
     https://localhost:5001/api/v1/convert
```

## 🔍 Monitoring and Logging

### Structured Logging

- Console logging for development
- File logging with daily rolling (in `/logs` directory)
- Configurable log levels
- Request tracing with correlation IDs

### Health Monitoring

- Background service monitors external API health
- Automatic retry with exponential backoff
- Cache invalidation on service issues

## 🛡️ Security Features

### API Key Authentication

- Required for all endpoints (except health checks)
- Rate limiting per API key
- Usage tracking and analytics
- Key expiration support

### Rate Limiting

- Configurable limits per API key
- Per-hour tracking with sliding windows
- HTTP 429 responses when limits exceeded
- IP address tracking for additional security

### Input Validation

- Model validation attributes
- Custom validation for currency codes
- Date range validation
- SQL injection prevention through EF Core

## 🔄 Background Services

### Rate Update Service

- **Real-time Updates**: Every 15 minutes
- **Historical Updates**: Daily batch updates
- **Error Handling**: Automatic retry with backoff
- **Cache Management**: Automatic cache invalidation

### Data Consistency

- Duplicate detection and prevention
- Data validation before storage
- Inconsistency logging and alerting

## 🎯 Design Decisions & Best Practices

### Error Handling Strategy

- Global exception middleware
- Specific exception types for different scenarios
- Detailed error logging without exposing internal details
- Consistent error response format

### Caching Strategy

- Memory caching for frequently accessed data
- Configurable TTL based on data type
- Cache-aside pattern implementation
- Automatic invalidation on data updates

### Database Design

- Composite indexes for optimal query performance
- Foreign key constraints for data integrity
- Decimal precision for accurate financial calculations
- Soft delete patterns where appropriate

### Performance Optimizations

- Async/await throughout the application
- Connection pooling for HTTP clients
- Database query optimization with indexes
- Memory-efficient data processing

## 📈 Scaling Considerations

### Horizontal Scaling

- Stateless design for multiple instance deployment
- External cache consideration (Redis) for distributed scenarios
- Database connection pooling configuration
- Load balancer friendly (health checks, graceful shutdown)

### Performance Tuning

- Database index optimization
- HTTP client connection reuse
- Background service interval tuning
- Cache expiration strategy optimization

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

For questions or support, please contact [your-email@example.com](mailto:your-email@example.com) or create an issue in the repository.

---

**Built with ❤️ using ASP.NET Core, Entity Framework Core, and modern C# practices.**
