using System;
using System.ComponentModel.DataAnnotations;

namespace CurrencyConverter.API.Domain.DTOs;

public class HistoricalRatesRequestDto
{
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string BaseCurrency { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string TargetCurrency { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class HistoricalRatesResponseDto
{
    public string BaseCurrency { get; set; } = string.Empty;
    public string TargetCurrency { get; set; } = string.Empty;
    public Dictionary<DateTime, decimal> Rates { get; set; } = new();
}
