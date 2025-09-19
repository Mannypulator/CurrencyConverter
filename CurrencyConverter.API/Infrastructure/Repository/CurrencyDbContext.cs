using System;
using CurrencyConverter.API.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace CurrencyConverter.API.Infrastructure.Repository;

public class CurrencyDbContext : DbContext
{
    public CurrencyDbContext(DbContextOptions<CurrencyDbContext> options) : base(options)
    {
    }

    public DbSet<Currency> Currencies { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<ApiKeyUsage> ApiKeyUsages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Currency configuration
        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).IsFixedLength();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // ExchangeRate configuration
        modelBuilder.Entity<ExchangeRate>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Create composite index for efficient querying
            entity.HasIndex(e => new { e.BaseCurrencyCode, e.TargetCurrencyCode, e.Date })
                  .IsUnique();

            entity.HasIndex(e => new { e.BaseCurrencyCode, e.TargetCurrencyCode, e.IsHistorical });
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.IsHistorical);

            // Configure relationships
            entity.HasOne(e => e.BaseCurrency)
                  .WithMany(c => c.BaseExchangeRates)
                  .HasForeignKey(e => e.BaseCurrencyCode)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetCurrency)
                  .WithMany(c => c.TargetExchangeRates)
                  .HasForeignKey(e => e.TargetCurrencyCode)
                  .OnDelete(DeleteBehavior.Restrict);

            // Configure precision for rate
            entity.Property(e => e.Rate)
                  .HasColumnType("decimal(18,8)");
        });

        // ApiKey configuration
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeyValue).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // ApiKeyUsage configuration
        modelBuilder.Entity<ApiKeyUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApiKeyId, e.RequestTime });
            entity.HasIndex(e => e.RequestTime);

            entity.HasOne(e => e.ApiKey)
                  .WithMany(k => k.UsageHistory)
                  .HasForeignKey(e => e.ApiKeyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed currencies
        var currencies = new[]
        {
                new Currency { Code = "USD", Name = "US Dollar", Symbol = "$" },
                new Currency { Code = "EUR", Name = "Euro", Symbol = "€" },
                new Currency { Code = "GBP", Name = "British Pound", Symbol = "£" },
                new Currency { Code = "JPY", Name = "Japanese Yen", Symbol = "¥" },
                new Currency { Code = "CAD", Name = "Canadian Dollar", Symbol = "C$" },
                new Currency { Code = "AUD", Name = "Australian Dollar", Symbol = "A$" },
                new Currency { Code = "CHF", Name = "Swiss Franc", Symbol = "CHF" },
                new Currency { Code = "CNY", Name = "Chinese Yuan", Symbol = "¥" }
            };

        modelBuilder.Entity<Currency>().HasData(currencies);

        // Seed API keys
        var apiKeys = new[]
        {
                new ApiKey
                {
                    Id = 1,
                    KeyValue = "demo-key-123456789",
                    Name = "Demo API Key",
                    RequestsPerHour = 1000,
                    IsActive = true
                },
                new ApiKey
                {
                    Id = 2,
                    KeyValue = "premium-key-987654321",
                    Name = "Premium API Key",
                    RequestsPerHour = 5000,
                    IsActive = true
                }
            };

        modelBuilder.Entity<ApiKey>().HasData(apiKeys);
    }
}
