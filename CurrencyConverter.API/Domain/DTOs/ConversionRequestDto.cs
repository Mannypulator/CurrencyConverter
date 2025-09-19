using System;
using System.ComponentModel.DataAnnotations;

namespace CurrencyConverter.API.Domain.DTOs;

public class ConversionRequestDto
{
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string FromCurrency { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string ToCurrency { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    public DateTime? Date { get; set; }
}

public class ConversionResponseDto
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public DateTime RateDate { get; set; }
    public DateTime ConversionTime { get; set; } = DateTime.UtcNow;
}
