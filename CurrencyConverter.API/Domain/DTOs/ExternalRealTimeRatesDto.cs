using System;

namespace CurrencyConverter.API.Domain.DTOs;

public class ExternalRealTimeRatesDto
{
    public string Base { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public Dictionary<string, decimal> Rates { get; set; } = new();
}

public class ExternalHistoricalRatesDto
{
    public string Base { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public Dictionary<string, decimal> Rates { get; set; } = new();
}
