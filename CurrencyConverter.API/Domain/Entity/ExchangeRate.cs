using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CurrencyConverter.API.Domain.Entity;

public class ExchangeRate
{
    [Key]
    public long Id { get; set; }

    [Required]
    [StringLength(3)]
    public string BaseCurrencyCode { get; set; } = string.Empty;

    [Required]
    [StringLength(3)]
    public string TargetCurrencyCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,8)")]
    public decimal Rate { get; set; }

    public DateTime Date { get; set; }

    public bool IsHistorical { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    
    [ForeignKey(nameof(BaseCurrencyCode))]
    public virtual Currency BaseCurrency { get; set; } = null!;

    [ForeignKey(nameof(TargetCurrencyCode))]
    public virtual Currency TargetCurrency { get; set; } = null!;
}
