using System;
using System.ComponentModel.DataAnnotations;

namespace CurrencyConverter.API.Domain.Entity;

public class ApiKey
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(256)]
    public string KeyValue { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int RequestsPerHour { get; set; } = 1000;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public virtual ICollection<ApiKeyUsage> UsageHistory { get; set; } = new List<ApiKeyUsage>();
}
