using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CurrencyConverter.API.Domain.Entity;

public class ApiKeyUsage
{
    [Key]
    public long Id { get; set; }

    public int ApiKeyId { get; set; }

    public DateTime RequestTime { get; set; } = DateTime.UtcNow;

    [StringLength(15)]
    public string IpAddress { get; set; } = string.Empty;

    [StringLength(200)]
    public string Endpoint { get; set; } = string.Empty;

    public int ResponseStatusCode { get; set; }

    [ForeignKey(nameof(ApiKeyId))]
    public virtual ApiKey ApiKey { get; set; } = null!;
}
