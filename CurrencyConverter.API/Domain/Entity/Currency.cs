using System;
using System.ComponentModel.DataAnnotations;

namespace CurrencyConverter.API.Domain.Entity;

public class Currency
{
    [Key]
    [StringLength(3)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(10)]
    public string Symbol { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    public virtual ICollection<ExchangeRate> BaseExchangeRates { get; set; } = new List<ExchangeRate>();
    public virtual ICollection<ExchangeRate> TargetExchangeRates { get; set; } = new List<ExchangeRate>();
}
